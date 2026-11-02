import { describe, expect, it, vi } from 'vitest'
import { ApiError } from './ApiError'
import { buildUrl, createHttpClient } from './httpClient'

const BASE_URL = 'https://api.test'

function jsonResponse(body: unknown, status = 200, contentType = 'application/json; charset=utf-8') {
  return new Response(JSON.stringify(body), { status, headers: { 'Content-Type': contentType } })
}

describe('buildUrl', () => {
  it('appends defined query values and omits empty ones', () => {
    const url = buildUrl(BASE_URL, '/pledgers', { page: 2, pageSize: 10, sortBy: undefined, sortOrder: '' })

    expect(url).toBe('https://api.test/pledgers?page=2&pageSize=10')
  })
})

describe('createHttpClient', () => {
  it('serializes request bodies as JSON and parses JSON responses', async () => {
    const fetchMock = vi.fn<typeof fetch>()
      .mockResolvedValueOnce(jsonResponse({ requestToken: 'test-token' }))
      .mockResolvedValue(jsonResponse({ id: 'a' }, 201))
    const client = createHttpClient(BASE_URL, fetchMock)

    const result = await client.post<{ id: string }>('/pledgers', { body: { pledgerName: 'Ada' } })

    expect(result).toEqual({ id: 'a' })
    const [url, init] = fetchMock.mock.calls[1]!
    expect(url).toBe('https://api.test/pledgers')
    expect(init?.method).toBe('POST')
    expect(init?.body).toBe('{"pledgerName":"Ada"}')
    expect(init?.headers).toMatchObject({ 'Content-Type': 'application/json', 'X-CSRF-TOKEN': 'test-token' })
    expect(init?.credentials).toBe('include')
  })

  it('omits the content type when there is no body', async () => {
    const fetchMock = vi.fn<typeof fetch>().mockResolvedValue(jsonResponse([]))
    const client = createHttpClient(BASE_URL, fetchMock)

    await client.get('/pledge-categories')

    const init = fetchMock.mock.calls[0]![1]
    expect(init?.body).toBeUndefined()
    expect(init?.headers).not.toHaveProperty('Content-Type')
  })

  it('resolves 204 No Content without reading a body', async () => {
    const fetchMock = vi.fn<typeof fetch>()
      .mockResolvedValueOnce(jsonResponse({ requestToken: 'test-token' }))
      .mockResolvedValue(new Response(null, { status: 204 }))
    const client = createHttpClient(BASE_URL, fetchMock)

    await expect(client.delete('/pledgers/1')).resolves.toBeUndefined()
  })

  it('maps application/problem+json errors to ApiError with ProblemDetails', async () => {
    const problem = {
      title: 'Relationship conflict',
      status: 409,
      detail: 'The operation conflicts with related accounting records.',
    }
    const fetchMock = vi.fn<typeof fetch>()
      .mockResolvedValueOnce(jsonResponse({ requestToken: 'test-token' }))
      .mockResolvedValue(jsonResponse(problem, 409, 'application/problem+json'))
    const client = createHttpClient(BASE_URL, fetchMock)

    const error = await client.delete('/pledgers/1').catch((caught: unknown) => caught)

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({ kind: 'http', status: 409, message: 'Relationship conflict', problem })
    expect((error as ApiError).isConflict).toBe(true)
  })

  it('does not parse non-JSON error bodies', async () => {
    const fetchMock = vi
      .fn<typeof fetch>()
      .mockResolvedValue(
        new Response('<html>stack trace</html>', { status: 500, headers: { 'Content-Type': 'text/html' } }),
      )
    const client = createHttpClient(BASE_URL, fetchMock)

    const error = (await client.get('/pledgers').catch((caught: unknown) => caught)) as ApiError

    expect(error.status).toBe(500)
    expect(error.problem).toBeUndefined()
    expect(error.message).toBe('Request failed with status 500.')
  })

  it('tolerates malformed JSON error bodies', async () => {
    const fetchMock = vi
      .fn<typeof fetch>()
      .mockResolvedValue(
        new Response('{not json', { status: 400, headers: { 'Content-Type': 'application/problem+json' } }),
      )
    const client = createHttpClient(BASE_URL, fetchMock)

    const error = (await client.get('/pledgers').catch((caught: unknown) => caught)) as ApiError

    expect(error.status).toBe(400)
    expect(error.problem).toBeUndefined()
  })

  it('reports network failures as connection errors', async () => {
    const fetchMock = vi.fn<typeof fetch>().mockRejectedValue(new TypeError('Failed to fetch'))
    const client = createHttpClient(BASE_URL, fetchMock)

    const error = (await client.get('/pledgers').catch((caught: unknown) => caught)) as ApiError

    expect(error).toBeInstanceOf(ApiError)
    expect(error.kind).toBe('network')
    expect(error.status).toBeUndefined()
  })

  it('rethrows aborts instead of reporting a connection error', async () => {
    const controller = new AbortController()
    controller.abort()
    const abortError = new DOMException('Aborted', 'AbortError')
    const fetchMock = vi.fn<typeof fetch>().mockRejectedValue(abortError)
    const client = createHttpClient(BASE_URL, fetchMock)

    await expect(client.get('/pledgers', { signal: controller.signal })).rejects.toBe(abortError)
  })

  it('does not send a mutation when the security token is missing', async () => {
    const fetchMock = vi.fn<typeof fetch>().mockResolvedValue(jsonResponse({}))
    const client = createHttpClient(BASE_URL, fetchMock)
    await expect(client.post('/pledgers', { body: {} })).rejects.toThrow('security token')
    expect(fetchMock).toHaveBeenCalledTimes(1)
  })

  it.each(['post', 'put', 'delete'] as const)('protects %s requests and uses a fresh token each time', async (method) => {
    const fetchMock = vi.fn<typeof fetch>()
      .mockResolvedValueOnce(jsonResponse({ requestToken: 'first' }))
      .mockResolvedValueOnce(new Response(null, { status: 204 }))
      .mockResolvedValueOnce(jsonResponse({ requestToken: 'second' }))
      .mockResolvedValueOnce(new Response(null, { status: 204 }))
    const client = createHttpClient(BASE_URL, fetchMock)
    await client[method]('/pledgers/1')
    await client[method]('/pledgers/1')
    expect(fetchMock.mock.calls[1]![1]?.headers).toHaveProperty('X-CSRF-TOKEN', 'first')
    expect(fetchMock.mock.calls[3]![1]?.headers).toHaveProperty('X-CSRF-TOKEN', 'second')
  })

  it('notifies the app of expired accounting requests but not failed login', async () => {
    const listener = vi.fn()
    window.addEventListener('accounting:session-expired', listener)
    const fetchMock = vi.fn<typeof fetch>().mockResolvedValue(jsonResponse({ title: 'Sign-in required' }, 401))
    const client = createHttpClient(BASE_URL, fetchMock)
    await expect(client.get('/auth/session')).rejects.toBeInstanceOf(ApiError)
    expect(listener).not.toHaveBeenCalled()
    await expect(client.get('/pledgers')).rejects.toBeInstanceOf(ApiError)
    expect(listener).toHaveBeenCalledTimes(1)
    window.removeEventListener('accounting:session-expired', listener)
  })
})

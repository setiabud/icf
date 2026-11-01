import { ApiError, type ProblemDetails } from './ApiError'
import { apiBaseUrl } from './config'

type QueryValue = string | number | boolean | undefined | null

export interface RequestOptions {
  query?: Record<string, QueryValue>
  body?: unknown
  signal?: AbortSignal
}

export interface HttpClient {
  get<T>(path: string, options?: RequestOptions): Promise<T>
  post<T>(path: string, options?: RequestOptions): Promise<T>
  put<T>(path: string, options?: RequestOptions): Promise<T>
  delete(path: string, options?: RequestOptions): Promise<void>
}

export function buildUrl(baseUrl: string, path: string, query?: Record<string, QueryValue>): string {
  const url = new URL(baseUrl + path)
  for (const [key, value] of Object.entries(query ?? {})) {
    if (value !== undefined && value !== null && value !== '') {
      url.searchParams.set(key, String(value))
    }
  }
  return url.toString()
}

function isJson(contentType: string | null): boolean {
  return contentType !== null && /application\/(problem\+)?json/i.test(contentType)
}

async function readProblem(response: Response): Promise<ProblemDetails | undefined> {
  if (!isJson(response.headers.get('content-type'))) {
    return undefined
  }
  try {
    const parsed: unknown = await response.json()
    return parsed !== null && typeof parsed === 'object' ? (parsed as ProblemDetails) : undefined
  } catch {
    return undefined
  }
}

export function createHttpClient(baseUrl: string, fetchImpl: typeof fetch = (...args) => fetch(...args)): HttpClient {
  async function send<T>(method: string, path: string, options: RequestOptions = {}): Promise<T> {
    const headers: Record<string, string> = { Accept: 'application/json, application/problem+json' }
    if (options.body !== undefined) {
      headers['Content-Type'] = 'application/json'
    }

    let response: Response
    try {
      response = await fetchImpl(buildUrl(baseUrl, path, options.query), {
        method,
        headers,
        body: options.body === undefined ? undefined : JSON.stringify(options.body),
        signal: options.signal,
      })
    } catch (error) {
      if (options.signal?.aborted) {
        throw error
      }
      throw new ApiError('network', `Unable to reach the API at ${baseUrl}.`)
    }

    if (!response.ok) {
      const problem = await readProblem(response)
      throw new ApiError(
        'http',
        problem?.title ?? `Request failed with status ${response.status}.`,
        response.status,
        problem,
      )
    }

    if (response.status === 204 || !isJson(response.headers.get('content-type'))) {
      return undefined as T
    }
    return (await response.json()) as T
  }

  return {
    get: (path, options) => send('GET', path, options),
    post: (path, options) => send('POST', path, options),
    put: (path, options) => send('PUT', path, options),
    delete: (path, options) => send<void>('DELETE', path, options),
  }
}

export const httpClient = createHttpClient(apiBaseUrl)

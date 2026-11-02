import { describe, expect, it, vi } from 'vitest'
import { createAccountingApi } from './accountingApi'
import { createHttpClient } from './httpClient'

const PLEDGE_ID = '6f1c2b9e-0d4a-4b8e-9a51-2f3c7d8e1a00'

function setup(...responses: Response[]) {
  const fetchMock = vi.fn<typeof fetch>().mockImplementation(async (input) => {
    if (String(input).endsWith('/auth/csrf')) return json({ requestToken: 'test-token' })
    const response = responses.shift()
    if (!response) throw new Error('Unexpected request')
    return response
  })
  return { fetchMock, api: createAccountingApi(createHttpClient('https://api.test', fetchMock)) }
}

const json = (body: unknown, status = 200) =>
  new Response(JSON.stringify(body), { status, headers: { 'Content-Type': 'application/json' } })

describe('accountingApi', () => {
  it('sends payment create requests with the documented body shape', async () => {
    const { fetchMock, api } = setup(json({ id: 'p1' }, 201))

    await api.pledgePayments.create({
      pledgeId: PLEDGE_ID,
      amount: 50.25,
      paymentDate: '2026-01-20T00:00:00',
      comment: null,
    })

    const [url, init] = fetchMock.mock.calls[1]!
    expect(url).toBe('https://api.test/pledge-payments')
    expect(JSON.parse(init!.body as string)).toEqual({
      pledgeId: PLEDGE_ID,
      amount: 50.25,
      paymentDate: '2026-01-20T00:00:00',
      comment: null,
    })
  })

  it('sends pledger updates with PUT to the pledger resource', async () => {
    const { fetchMock, api } = setup(json({ id: PLEDGE_ID }))

    await api.pledgers.update(PLEDGE_ID, {
      pledgerName: 'Ada',
      active: false,
      email: 'ada@example.org',
      enableNotification: true,
    })

    const [url, init] = fetchMock.mock.calls[1]!
    expect(url).toBe(`https://api.test/pledgers/${PLEDGE_ID}`)
    expect(init?.method).toBe('PUT')
    expect(JSON.parse(init!.body as string)).toEqual({
      pledgerName: 'Ada',
      active: false,
      email: 'ada@example.org',
      enableNotification: true,
    })
  })

  it('lists pledges by category with paging and sorting parameters', async () => {
    const { fetchMock, api } = setup(json({ items: [], page: 2, pageSize: 20, totalCount: 0, totalPages: 0 }))

    await api.pledges.listByCategory('cat-1', { page: 2, pageSize: 20, sortBy: 'Amount', sortOrder: 'Ascending' })

    expect(fetchMock.mock.calls[0]![0]).toBe(
      'https://api.test/pledges/pledge-category/cat-1?page=2&pageSize=20&sortBy=Amount&sortOrder=Ascending',
    )
  })

  it('loads every pledger page for option lists', async () => {
    const { fetchMock, api } = setup(
      json({ items: [{ id: 'a' }], page: 1, pageSize: 100, totalCount: 2, totalPages: 2 }),
      json({ items: [{ id: 'b' }], page: 2, pageSize: 100, totalCount: 2, totalPages: 2 }),
    )

    const pledgers = await api.pledgers.listAll()

    expect(pledgers.map((pledger) => pledger.id)).toEqual(['a', 'b'])
    expect(fetchMock).toHaveBeenCalledTimes(2)
    expect(fetchMock.mock.calls[1]![0]).toBe(
      'https://api.test/pledgers?page=2&pageSize=100&sortBy=PledgerName&sortOrder=Ascending',
    )
  })
})

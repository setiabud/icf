import { httpClient, type HttpClient } from './httpClient'
import type {
  CreatePledgeCategoryRequest,
  CreatePledgePaymentRequest,
  CreatePledgerRequest,
  Guid,
  PagedResponse,
  PageQuery,
  Pledge,
  PledgeCategory,
  PledgePayment,
  PledgePaymentSortBy,
  Pledger,
  PledgerSortBy,
  PledgeSortBy,
  SavePledgeRequest,
  UpdatePledgeCategoryRequest,
  UpdatePledgePaymentRequest,
  UpdatePledgerRequest,
} from './types'

/** Largest page size the API accepts; used when loading complete option lists. */
export const MAX_PAGE_SIZE = 100

const id = (value: Guid) => encodeURIComponent(value)

function pageQuery<TSortBy extends string>(query: PageQuery<TSortBy> = {}) {
  return { page: query.page, pageSize: query.pageSize, sortBy: query.sortBy, sortOrder: query.sortOrder }
}

async function loadAllPages<TItem>(loadPage: (page: number) => Promise<PagedResponse<TItem>>): Promise<TItem[]> {
  const items: TItem[] = []
  let page = 1
  let totalPages = 1
  do {
    const result = await loadPage(page)
    items.push(...result.items)
    totalPages = result.totalPages
    page += 1
  } while (page <= totalPages)
  return items
}

export function createAccountingApi(http: HttpClient) {
  const pledgers = {
    list: (query?: PageQuery<PledgerSortBy>, signal?: AbortSignal) =>
      http.get<PagedResponse<Pledger>>('/pledgers', { query: pageQuery(query), signal }),
    listAll: (signal?: AbortSignal) =>
      loadAllPages((page) =>
        pledgers.list({ page, pageSize: MAX_PAGE_SIZE, sortBy: 'PledgerName', sortOrder: 'Ascending' }, signal),
      ),
    get: (pledgerId: Guid, signal?: AbortSignal) => http.get<Pledger>(`/pledgers/${id(pledgerId)}`, { signal }),
    create: (body: CreatePledgerRequest) => http.post<Pledger>('/pledgers', { body }),
    update: (pledgerId: Guid, body: UpdatePledgerRequest) => http.put<Pledger>(`/pledgers/${id(pledgerId)}`, { body }),
    remove: (pledgerId: Guid) => http.delete(`/pledgers/${id(pledgerId)}`),
  }

  const pledgeCategories = {
    list: (signal?: AbortSignal) => http.get<PledgeCategory[]>('/pledge-categories', { signal }),
    get: (categoryId: Guid, signal?: AbortSignal) =>
      http.get<PledgeCategory>(`/pledge-categories/${id(categoryId)}`, { signal }),
    create: (body: CreatePledgeCategoryRequest) => http.post<PledgeCategory>('/pledge-categories', { body }),
    update: (categoryId: Guid, body: UpdatePledgeCategoryRequest) =>
      http.put<PledgeCategory>(`/pledge-categories/${id(categoryId)}`, { body }),
    remove: (categoryId: Guid) => http.delete(`/pledge-categories/${id(categoryId)}`),
  }

  const pledges = {
    listByPledger: (pledgerId: Guid, query?: PageQuery<PledgeSortBy>, signal?: AbortSignal) =>
      http.get<PagedResponse<Pledge>>(`/pledges/pledger/${id(pledgerId)}`, { query: pageQuery(query), signal }),
    listByCategory: (categoryId: Guid, query?: PageQuery<PledgeSortBy>, signal?: AbortSignal) =>
      http.get<PagedResponse<Pledge>>(`/pledges/pledge-category/${id(categoryId)}`, {
        query: pageQuery(query),
        signal,
      }),
    listAllByPledger: (pledgerId: Guid, signal?: AbortSignal) =>
      loadAllPages((page) =>
        pledges.listByPledger(
          pledgerId,
          { page, pageSize: MAX_PAGE_SIZE, sortBy: 'Year', sortOrder: 'Descending' },
          signal,
        ),
      ),
    get: (pledgeId: Guid, signal?: AbortSignal) => http.get<Pledge>(`/pledges/${id(pledgeId)}`, { signal }),
    create: (body: SavePledgeRequest) => http.post<Pledge>('/pledges', { body }),
    update: (pledgeId: Guid, body: SavePledgeRequest) => http.put<Pledge>(`/pledges/${id(pledgeId)}`, { body }),
    remove: (pledgeId: Guid) => http.delete(`/pledges/${id(pledgeId)}`),
  }

  const pledgePayments = {
    listByPledge: (pledgeId: Guid, query?: PageQuery<PledgePaymentSortBy>, signal?: AbortSignal) =>
      http.get<PagedResponse<PledgePayment>>(`/pledge-payments/pledge/${id(pledgeId)}`, {
        query: pageQuery(query),
        signal,
      }),
    get: (paymentId: Guid, signal?: AbortSignal) =>
      http.get<PledgePayment>(`/pledge-payments/${id(paymentId)}`, { signal }),
    create: (body: CreatePledgePaymentRequest) => http.post<PledgePayment>('/pledge-payments', { body }),
    update: (paymentId: Guid, body: UpdatePledgePaymentRequest) =>
      http.put<PledgePayment>(`/pledge-payments/${id(paymentId)}`, { body }),
    remove: (paymentId: Guid) => http.delete(`/pledge-payments/${id(paymentId)}`),
  }

  return { pledgers, pledgeCategories, pledges, pledgePayments }
}

export type AccountingApi = ReturnType<typeof createAccountingApi>

export const accountingApi = createAccountingApi(httpClient)

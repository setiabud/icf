/** A GUID serialized as a string, e.g. "6f1c2b9e-0d4a-4b8e-9a51-2f3c7d8e1a00". */
export type Guid = string

/** A UTC timestamp serialized as an ISO 8601 string. */
export type IsoDateTime = string

export type SortOrder = 'Ascending' | 'Descending'

export interface PagedResponse<TItem> {
  items: TItem[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
}

export interface PageQuery<TSortBy extends string> {
  page?: number
  pageSize?: number
  sortBy?: TSortBy
  sortOrder?: SortOrder
}

export interface Pledger {
  id: Guid
  pledgerName: string
  active: boolean
  email: string | null
  enableNotification: boolean
  createdAt: IsoDateTime
  updatedAt: IsoDateTime
}

export interface CreatePledgerRequest {
  pledgerName: string
  email?: string | null
  enableNotification?: boolean
}

export interface UpdatePledgerRequest {
  pledgerName: string
  active: boolean
  email: string | null
  enableNotification: boolean
}

export type PledgerSortBy = 'PledgerName'

export interface PledgeCategory {
  id: Guid
  categoryName: string
  active: boolean
  createdAt: IsoDateTime
  updatedAt: IsoDateTime
}

export interface CreatePledgeCategoryRequest {
  categoryName: string
}

export interface UpdatePledgeCategoryRequest {
  categoryName: string
  active: boolean
}

export interface Pledge {
  id: Guid
  year: number
  amount: number
  pledgerId: Guid
  pledgeCategoryId: Guid
  comment: string | null
  createdAt: IsoDateTime
  updatedAt: IsoDateTime
}

export interface SavePledgeRequest {
  year: number
  amount: number
  pledgerId: Guid
  pledgeCategoryId: Guid
  comment?: string | null
}

export type PledgeSortBy = 'Year' | 'Amount'

export interface PledgePayment {
  id: Guid
  pledgeId: Guid
  amount: number
  /** Stored and returned exactly as sent, with no time-zone conversion. */
  paymentDate: string
  comment: string | null
  active: boolean
  createdAt: IsoDateTime
  updatedAt: IsoDateTime
}

export interface CreatePledgePaymentRequest {
  pledgeId: Guid
  amount: number
  paymentDate: string
  comment?: string | null
}

export interface UpdatePledgePaymentRequest extends CreatePledgePaymentRequest {
  active: boolean
}

export type PledgePaymentSortBy = 'PaymentDate' | 'Amount'

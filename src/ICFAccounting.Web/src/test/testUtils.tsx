import { render, type RenderResult } from '@testing-library/react'
import type { ReactElement } from 'react'
import { vi } from 'vitest'
import { ApiProvider } from '../api/ApiContext'
import type { AccountingApi } from '../api/accountingApi'
import type { PagedResponse, Pledge, PledgeCategory, Pledger } from '../api/types'
import { NotificationProvider } from '../components/Notifications'

const TIMESTAMP = '2026-01-01T00:00:00Z'

export function pledger(id: string, pledgerName: string, active = true): Pledger {
  return { id, pledgerName, active, createdAt: TIMESTAMP, updatedAt: TIMESTAMP }
}

export function category(id: string, categoryName: string, active = true): PledgeCategory {
  return { id, categoryName, active, createdAt: TIMESTAMP, updatedAt: TIMESTAMP }
}

export function pledge(id: string, values: Partial<Pledge> = {}): Pledge {
  return {
    id,
    year: 2026,
    amount: 1200,
    pledgerId: 'pledger-1',
    pledgeCategoryId: 'category-1',
    comment: null,
    createdAt: TIMESTAMP,
    updatedAt: TIMESTAMP,
    ...values,
  }
}

export function page<T>(items: T[]): PagedResponse<T> {
  return { items, page: 1, pageSize: 20, totalCount: items.length, totalPages: items.length > 0 ? 1 : 0 }
}

/** Creates an API double whose methods reject unless a test provides an implementation. */
export function createFakeApi() {
  const unexpected = () => vi.fn().mockRejectedValue(new Error('Unexpected API call'))
  return {
    pledgers: {
      list: unexpected(),
      listAll: unexpected(),
      get: unexpected(),
      create: unexpected(),
      update: unexpected(),
      remove: unexpected(),
    },
    pledgeCategories: {
      list: unexpected(),
      get: unexpected(),
      create: unexpected(),
      update: unexpected(),
      remove: unexpected(),
    },
    pledges: {
      listByPledger: unexpected(),
      listByCategory: unexpected(),
      listAllByPledger: unexpected(),
      get: unexpected(),
      create: unexpected(),
      update: unexpected(),
      remove: unexpected(),
    },
    pledgePayments: {
      listByPledge: unexpected(),
      get: unexpected(),
      create: unexpected(),
      update: unexpected(),
      remove: unexpected(),
    },
  } satisfies Record<keyof AccountingApi, Record<string, unknown>>
}

export type FakeApi = ReturnType<typeof createFakeApi>

/** Renders with the fake API and notifications; providers persist across `rerender`. */
export function renderWithApi(ui: ReactElement, api: FakeApi): RenderResult {
  return render(ui, {
    wrapper: ({ children }) => (
      <ApiProvider api={api as unknown as AccountingApi}>
        <NotificationProvider>{children}</NotificationProvider>
      </ApiProvider>
    ),
  })
}

export function deferred<T>() {
  let resolve!: (value: T) => void
  let reject!: (reason: unknown) => void
  const promise = new Promise<T>((onResolve, onReject) => {
    resolve = onResolve
    reject = onReject
  })
  return { promise, resolve, reject }
}

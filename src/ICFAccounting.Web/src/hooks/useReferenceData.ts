import { useMemo } from 'react'
import { useApi } from '../api/ApiContext'
import type { Guid, Pledger, PledgeCategory } from '../api/types'
import { useAsyncData, type AsyncResource, type AsyncState } from './useAsyncData'

export interface ReferenceData {
  pledgers: Pledger[]
  categories: PledgeCategory[]
  pledgerName: (id: Guid) => string
  categoryName: (id: Guid) => string
}

/** Loads every pledger (across pages) and every pledge category for selectors and name lookups. */
export function useReferenceData(): AsyncResource<ReferenceData> {
  const api = useApi()
  const { state, reload } = useAsyncData(
    (signal) => Promise.all([api.pledgers.listAll(signal), api.pledgeCategories.list(signal)]),
    [api],
  )

  const referenceData = useMemo<ReferenceData | undefined>(() => {
    if (!state.data) {
      return undefined
    }
    const [pledgers, categories] = state.data
    const pledgerNames = new Map(pledgers.map((pledger) => [pledger.id, pledger.pledgerName]))
    const categoryNames = new Map(categories.map((category) => [category.id, category.categoryName]))
    return {
      pledgers,
      categories,
      pledgerName: (id) => pledgerNames.get(id) ?? 'Unknown pledger',
      categoryName: (id) => categoryNames.get(id) ?? 'Unknown category',
    }
  }, [state.data])

  const mappedState = useMemo<AsyncState<ReferenceData>>(() => {
    if (state.status === 'error') {
      return { status: 'error', data: referenceData, error: state.error }
    }
    if (state.status === 'success' && referenceData) {
      return { status: 'success', data: referenceData, error: undefined }
    }
    return { status: 'loading', data: referenceData, error: undefined }
  }, [state, referenceData])
  return { state: mappedState, reload }
}

export function optionLabel(name: string, active: boolean): string {
  return active ? name : `${name} (inactive)`
}

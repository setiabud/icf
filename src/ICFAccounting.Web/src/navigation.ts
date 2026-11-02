import { useEffect, useState } from 'react'
import type { PaymentSelection } from './features/payments/PaymentsView'
import type { PledgeParent } from './features/pledges/PledgeParent'

export type Section = 'pledgers' | 'categories' | 'pledges' | 'payments'
export interface AppRoute {
  section: Section
  pledgeParent: PledgeParent
  paymentSelection: PaymentSelection
}

export function readRoute(hash: string): AppRoute {
  const [path, search] = hash.replace(/^#/, '').split('?')
  const section = path?.replace(/^\//, '')
  const query = new URLSearchParams(search)
  return {
    section: section === 'categories' || section === 'pledges' || section === 'payments' ? section : 'pledgers',
    pledgeParent: { kind: query.get('kind') === 'category' ? 'category' : 'pledger', id: query.get('parentId') ?? '' },
    paymentSelection: { pledgerId: query.get('pledgerId') ?? '', pledgeId: query.get('pledgeId') ?? '' },
  }
}

export function routeHash(route: AppRoute): string {
  const query = new URLSearchParams()
  if (route.section === 'pledges' && route.pledgeParent.id) {
    query.set('kind', route.pledgeParent.kind)
    query.set('parentId', route.pledgeParent.id)
  }
  if (route.section === 'payments') {
    if (route.paymentSelection.pledgerId) query.set('pledgerId', route.paymentSelection.pledgerId)
    if (route.paymentSelection.pledgeId) query.set('pledgeId', route.paymentSelection.pledgeId)
  }
  return `#/${route.section}${query.size ? `?${query}` : ''}`
}

export function useAppRoute() {
  const [route, setRoute] = useState(() => readRoute(window.location.hash))
  useEffect(() => {
    const update = () => {
      if (window.location.hash === '#main') return
      setRoute(readRoute(window.location.hash))
    }
    window.addEventListener('hashchange', update)
    return () => window.removeEventListener('hashchange', update)
  }, [])

  function navigate(next: AppRoute) {
    window.location.hash = routeHash(next)
    setRoute(next)
  }

  return { route, navigate }
}

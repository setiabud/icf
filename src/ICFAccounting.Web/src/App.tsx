import { useState } from 'react'
import { apiBaseUrl } from './api/config'
import type { Pledge } from './api/types'
import { NotificationProvider } from './components/Notifications'
import { CategoriesView } from './features/categories/CategoriesView'
import { PaymentsView, type PaymentSelection } from './features/payments/PaymentsView'
import type { PledgeParent } from './features/pledges/PledgeParent'
import { PledgesView } from './features/pledges/PledgesView'
import { PledgersView } from './features/pledgers/PledgersView'

type Section = 'pledgers' | 'categories' | 'pledges' | 'payments'

const SECTIONS: { id: Section; label: string }[] = [
  { id: 'pledgers', label: 'Pledgers' },
  { id: 'categories', label: 'Categories' },
  { id: 'pledges', label: 'Pledges' },
  { id: 'payments', label: 'Payments' },
]

export default function App() {
  const [section, setSection] = useState<Section>('pledgers')
  const [pledgeParent, setPledgeParent] = useState<PledgeParent>({ kind: 'pledger', id: '' })
  const [paymentSelection, setPaymentSelection] = useState<PaymentSelection>({ pledgerId: '', pledgeId: '' })

  function showPledges(parent: PledgeParent) {
    setPledgeParent(parent)
    setSection('pledges')
  }

  function showPayments(pledge: Pledge) {
    setPaymentSelection({ pledgerId: pledge.pledgerId, pledgeId: pledge.id })
    setSection('payments')
  }

  return (
    <NotificationProvider>
      <a className="skip-link" href="#main">
        Skip to content
      </a>
      <header className="app-header">
        <div className="app-header__inner">
          <h1 className="app-title">ICF Accounting</h1>
          <nav aria-label="Primary">
            <ul className="nav">
              {SECTIONS.map((item) => (
                <li key={item.id}>
                  <button
                    type="button"
                    className="nav__link"
                    aria-current={section === item.id ? 'page' : undefined}
                    onClick={() => setSection(item.id)}
                  >
                    {item.label}
                  </button>
                </li>
              ))}
            </ul>
          </nav>
        </div>
      </header>
      <main id="main" className="app-main" tabIndex={-1}>
        {section === 'pledgers' && <PledgersView onViewPledges={(id) => showPledges({ kind: 'pledger', id })} />}
        {section === 'categories' && <CategoriesView onViewPledges={(id) => showPledges({ kind: 'category', id })} />}
        {section === 'pledges' && (
          <PledgesView parent={pledgeParent} onParentChange={setPledgeParent} onViewPayments={showPayments} />
        )}
        {section === 'payments' && (
          <PaymentsView selection={paymentSelection} onSelectionChange={setPaymentSelection} />
        )}
      </main>
      <footer className="app-footer">API: {apiBaseUrl}</footer>
    </NotificationProvider>
  )
}

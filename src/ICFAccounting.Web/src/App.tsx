import { apiBaseUrl } from './api/config'
import type { Pledge } from './api/types'
import { NotificationProvider } from './components/Notifications'
import { CategoriesView } from './features/categories/CategoriesView'
import { PaymentsView, type PaymentSelection } from './features/payments/PaymentsView'
import type { PledgeParent } from './features/pledges/PledgeParent'
import { PledgesView } from './features/pledges/PledgesView'
import { PledgersView } from './features/pledgers/PledgersView'
import { LoginForm } from './auth/LoginForm'
import { useSession } from './auth/useSession'
import { ErrorAlert } from './components/Alert'
import { useAppRoute, type Section } from './navigation'

const SECTIONS: { id: Section; label: string }[] = [
  { id: 'pledgers', label: 'Pledgers' },
  { id: 'categories', label: 'Categories' },
  { id: 'pledges', label: 'Pledges' },
  { id: 'payments', label: 'Payments' },
]

export default function App() {
  const session = useSession()
  const { route, navigate } = useAppRoute()
  const { section, pledgeParent, paymentSelection } = route
  const signedIn = session.state.status === 'authenticated'

  function showPledges(parent: PledgeParent) {
    navigate({ ...route, section: 'pledges', pledgeParent: parent })
  }

  function showPayments(pledge: Pledge) {
    navigate({ ...route, section: 'payments', paymentSelection: { pledgerId: pledge.pledgerId, pledgeId: pledge.id } })
  }

  return (
    <NotificationProvider>
      <a className="skip-link" href="#main" onClick={(event) => {
        event.preventDefault()
        document.getElementById('main')?.focus()
      }}>
        Skip to content
      </a>
      <header className="app-header">
        <div className="app-header__inner">
          <h1 className="app-title">ICF Accounting</h1>
          {signedIn && <nav aria-label="Primary">
            <ul className="nav">
              {SECTIONS.map((item) => (
                <li key={item.id}>
                  <button
                    type="button"
                    className="nav__link"
                    aria-current={section === item.id ? 'page' : undefined}
                    onClick={() => navigate({ ...route, section: item.id })}
                  >
                    {item.label}
                  </button>
                </li>
              ))}
            </ul>
          </nav>}
          {session.state.status === 'authenticated' && (
            <div className="account-menu">
              {session.state.user.profileImageUrl && (
                <img className="account-avatar" src={session.state.user.profileImageUrl} alt="" referrerPolicy="no-referrer" />
              )}
              <span>{session.state.user.name}</span>
              <button type="button" className="button button--secondary" disabled={session.loggingOut}
                onClick={() => { void session.logout() }}>
                {session.loggingOut ? 'Signing out…' : 'Sign out'}
              </button>
            </div>
          )}
        </div>
      </header>
      <main id="main" className="app-main" tabIndex={-1}>
        {session.state.status === 'loading' && <p role="status">Restoring your session…</p>}
        {session.state.status === 'error' && (
          <ErrorAlert error={session.state.error} action="restore your session" onRetry={() => { void session.restore() }} />
        )}
        {session.state.status === 'anonymous' && <LoginForm onLogin={session.login} message={session.message} />}
        {session.logoutError !== undefined && signedIn && <ErrorAlert error={session.logoutError} action="sign out" />}
        {signedIn && section === 'pledgers' && <PledgersView onViewPledges={(id) => showPledges({ kind: 'pledger', id })} />}
        {signedIn && section === 'categories' && <CategoriesView onViewPledges={(id) => showPledges({ kind: 'category', id })} />}
        {signedIn && section === 'pledges' && (
          <PledgesView parent={pledgeParent} onParentChange={(parent) => navigate({ ...route, pledgeParent: parent })} onViewPayments={showPayments} />
        )}
        {signedIn && section === 'payments' && (
          <PaymentsView selection={paymentSelection} onSelectionChange={(selection: PaymentSelection) => navigate({ ...route, paymentSelection: selection })} />
        )}
      </main>
      <footer className="app-footer">API: {apiBaseUrl}</footer>
    </NotificationProvider>
  )
}

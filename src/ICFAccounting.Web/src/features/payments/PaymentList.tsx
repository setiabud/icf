import { useState } from 'react'
import { useApi } from '../../api/ApiContext'
import type { Pledge, PledgePayment, PledgePaymentSortBy, SortOrder } from '../../api/types'
import { ErrorAlert } from '../../components/Alert'
import { ConfirmDeleteDialog } from '../../components/ConfirmDeleteDialog'
import { Modal } from '../../components/Modal'
import { useNotify } from '../../components/Notifications'
import { Pagination } from '../../components/Pagination'
import { EmptyState, LoadingState, StatusBadge } from '../../components/States'
import { useAsyncData } from '../../hooks/useAsyncData'
import { formatAmount, formatPaymentDate } from '../../lib/format'
import { PaymentForm } from './PaymentForm'

const PAGE_SIZE = 20

type Editing = { mode: 'create' } | { mode: 'edit'; payment: PledgePayment }

interface PaymentListProps {
  pledge: Pledge
  pledgeDescription: string
}

export function PaymentList({ pledge, pledgeDescription }: PaymentListProps) {
  const api = useApi()
  const notify = useNotify()
  const [page, setPage] = useState(1)
  const [sortBy, setSortBy] = useState<PledgePaymentSortBy>('PaymentDate')
  const [sortOrder, setSortOrder] = useState<SortOrder>('Descending')
  const [editing, setEditing] = useState<Editing>()
  const [deleting, setDeleting] = useState<PledgePayment>()

  const { state, reload } = useAsyncData(
    (signal) => api.pledgePayments.listByPledge(pledge.id, { page, pageSize: PAGE_SIZE, sortBy, sortOrder }, signal),
    [api, pledge.id, page, sortBy, sortOrder],
  )
  const result = state.data

  function describePayment(payment: PledgePayment): string {
    return `payment of ${formatAmount(payment.amount)} on ${formatPaymentDate(payment.paymentDate)}`
  }

  function handleSaved(payment: PledgePayment) {
    notify(
      editing?.mode === 'edit' ? `Saved the ${describePayment(payment)}.` : `Recorded a ${describePayment(payment)}.`,
    )
    setEditing(undefined)
    reload()
  }

  function handleDeleted(payment: PledgePayment) {
    notify(`Deleted the ${describePayment(payment)}.`)
    setDeleting(undefined)
    if (result && result.items.length === 1 && page > 1) {
      setPage(page - 1)
    } else {
      reload()
    }
  }

  return (
    <div className="list-panel">
      <div className="section-header section-header--sub">
        <div>
          <h3>{pledgeDescription}</h3>
          <p className="muted">Pledged amount: {formatAmount(pledge.amount)}</p>
        </div>
        <div className="toolbar">
          <label className="inline-field">
            Sort by
            <select
              value={sortBy}
              onChange={(event) => {
                setSortBy(event.target.value as PledgePaymentSortBy)
                setPage(1)
              }}
            >
              <option value="PaymentDate">Payment date</option>
              <option value="Amount">Amount</option>
            </select>
          </label>
          <label className="inline-field">
            Order
            <select
              value={sortOrder}
              onChange={(event) => {
                setSortOrder(event.target.value as SortOrder)
                setPage(1)
              }}
            >
              <option value="Descending">Descending</option>
              <option value="Ascending">Ascending</option>
            </select>
          </label>
          <button type="button" className="button button--primary" onClick={() => setEditing({ mode: 'create' })}>
            Record payment
          </button>
        </div>
      </div>

      {state.status === 'error' && <ErrorAlert error={state.error} action="load payments" onRetry={reload} />}
      {state.status === 'loading' && !result && <LoadingState label="Loading payments…" />}
      {result && result.items.length === 0 && (
        <EmptyState>
          <p>No payments recorded for this pledge.</p>
        </EmptyState>
      )}
      {result && result.items.length > 0 && (
        <>
          <div className="table-wrap" aria-busy={state.status === 'loading'}>
            <table>
              <thead>
                <tr>
                  <th scope="col">Payment date</th>
                  <th scope="col" className="numeric">
                    Amount
                  </th>
                  <th scope="col">Status</th>
                  <th scope="col">Comment</th>
                  <th scope="col">
                    <span className="visually-hidden">Actions</span>
                  </th>
                </tr>
              </thead>
              <tbody>
                {result.items.map((payment) => (
                  <tr key={payment.id}>
                    <td data-label="Payment date">
                      <time dateTime={payment.paymentDate}>{formatPaymentDate(payment.paymentDate)}</time>
                    </td>
                    <td data-label="Amount" className="numeric">
                      {formatAmount(payment.amount)}
                    </td>
                    <td data-label="Status">
                      <StatusBadge active={payment.active} />
                    </td>
                    <td data-label="Comment" className="comment">
                      {payment.comment ?? <span className="muted">—</span>}
                    </td>
                    <td className="row-actions">
                      <button
                        type="button"
                        className="button button--link"
                        onClick={() => setEditing({ mode: 'edit', payment })}
                        aria-label={`Edit the ${describePayment(payment)}`}
                      >
                        Edit
                      </button>
                      <button
                        type="button"
                        className="button button--link danger"
                        onClick={() => setDeleting(payment)}
                        aria-label={`Delete the ${describePayment(payment)}`}
                      >
                        Delete
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          <Pagination
            page={result.page}
            totalPages={result.totalPages}
            totalCount={result.totalCount}
            disabled={state.status === 'loading'}
            onPageChange={setPage}
          />
        </>
      )}

      {editing && (
        <Modal
          title={editing.mode === 'edit' ? 'Edit payment' : 'Record payment'}
          onClose={() => setEditing(undefined)}
        >
          <PaymentForm
            pledgeId={pledge.id}
            payment={editing.mode === 'edit' ? editing.payment : undefined}
            onSubmit={({ active, ...createRequest }) =>
              editing.mode === 'edit'
                ? api.pledgePayments.update(editing.payment.id, { ...createRequest, active })
                : api.pledgePayments.create(createRequest)
            }
            onSaved={handleSaved}
            onCancel={() => setEditing(undefined)}
          />
        </Modal>
      )}

      {deleting && (
        <ConfirmDeleteDialog
          title="Delete payment"
          itemDescription={`the ${describePayment(deleting)}`}
          relationshipHint="Deleting a payment removes it from this pledge's payment history."
          onDelete={() => api.pledgePayments.remove(deleting.id)}
          onDeleted={() => handleDeleted(deleting)}
          onCancel={() => setDeleting(undefined)}
        />
      )}
    </div>
  )
}

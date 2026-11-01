import { useState } from 'react'
import { useApi } from '../../api/ApiContext'
import type { Guid, Pledge, PledgeSortBy, SortOrder } from '../../api/types'
import { ErrorAlert } from '../../components/Alert'
import { ConfirmDeleteDialog } from '../../components/ConfirmDeleteDialog'
import { Modal } from '../../components/Modal'
import { useNotify } from '../../components/Notifications'
import { Pagination } from '../../components/Pagination'
import { EmptyState, LoadingState } from '../../components/States'
import { useAsyncData } from '../../hooks/useAsyncData'
import type { ReferenceData } from '../../hooks/useReferenceData'
import { formatAmount } from '../../lib/format'
import { PledgeForm } from './PledgeForm'
import type { PledgeParentKind } from './PledgeParent'

const PAGE_SIZE = 20

type Editing = { mode: 'create' } | { mode: 'edit'; pledge: Pledge }

interface PledgeListProps {
  parent: { kind: PledgeParentKind; id: Guid }
  referenceData: ReferenceData
  onViewPayments: (pledge: Pledge) => void
}

export function PledgeList({ parent, referenceData, onViewPayments }: PledgeListProps) {
  const api = useApi()
  const notify = useNotify()
  const [page, setPage] = useState(1)
  const [sortBy, setSortBy] = useState<PledgeSortBy>('Year')
  const [sortOrder, setSortOrder] = useState<SortOrder>('Descending')
  const [editing, setEditing] = useState<Editing>()
  const [deleting, setDeleting] = useState<Pledge>()

  const { state, reload } = useAsyncData(
    (signal) => {
      const query = { page, pageSize: PAGE_SIZE, sortBy, sortOrder }
      return parent.kind === 'pledger'
        ? api.pledges.listByPledger(parent.id, query, signal)
        : api.pledges.listByCategory(parent.id, query, signal)
    },
    [api, parent.kind, parent.id, page, sortBy, sortOrder],
  )
  const result = state.data
  const parentName =
    parent.kind === 'pledger' ? referenceData.pledgerName(parent.id) : referenceData.categoryName(parent.id)

  function describePledge(pledge: Pledge): string {
    return `${pledge.year} ${referenceData.categoryName(pledge.pledgeCategoryId)} pledge from ${referenceData.pledgerName(pledge.pledgerId)}`
  }

  function handleSaved(pledge: Pledge) {
    notify(editing?.mode === 'edit' ? `Saved the ${describePledge(pledge)}.` : `Created the ${describePledge(pledge)}.`)
    setEditing(undefined)
    reload()
  }

  function handleDeleted(pledge: Pledge) {
    notify(`Deleted the ${describePledge(pledge)}.`)
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
        <h3>{parentName}</h3>
        <div className="toolbar">
          <label className="inline-field">
            Sort by
            <select
              value={sortBy}
              onChange={(event) => {
                setSortBy(event.target.value as PledgeSortBy)
                setPage(1)
              }}
            >
              <option value="Year">Year</option>
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
            New pledge
          </button>
        </div>
      </div>

      {state.status === 'error' && <ErrorAlert error={state.error} action="load pledges" onRetry={reload} />}
      {state.status === 'loading' && !result && <LoadingState label="Loading pledges…" />}
      {result && result.items.length === 0 && (
        <EmptyState>
          <p>No pledges recorded for {parentName}.</p>
        </EmptyState>
      )}
      {result && result.items.length > 0 && (
        <>
          <div className="table-wrap" aria-busy={state.status === 'loading'}>
            <table>
              <thead>
                <tr>
                  <th scope="col">Year</th>
                  <th scope="col" className="numeric">
                    Amount
                  </th>
                  <th scope="col">{parent.kind === 'pledger' ? 'Category' : 'Pledger'}</th>
                  <th scope="col">Comment</th>
                  <th scope="col">
                    <span className="visually-hidden">Actions</span>
                  </th>
                </tr>
              </thead>
              <tbody>
                {result.items.map((pledge) => (
                  <tr key={pledge.id}>
                    <td data-label="Year">{pledge.year}</td>
                    <td data-label="Amount" className="numeric">
                      {formatAmount(pledge.amount)}
                    </td>
                    <td data-label={parent.kind === 'pledger' ? 'Category' : 'Pledger'}>
                      {parent.kind === 'pledger'
                        ? referenceData.categoryName(pledge.pledgeCategoryId)
                        : referenceData.pledgerName(pledge.pledgerId)}
                    </td>
                    <td data-label="Comment" className="comment">
                      {pledge.comment ?? <span className="muted">—</span>}
                    </td>
                    <td className="row-actions">
                      <button
                        type="button"
                        className="button button--link"
                        onClick={() => onViewPayments(pledge)}
                        aria-label={`Payments for the ${describePledge(pledge)}`}
                      >
                        Payments
                      </button>
                      <button
                        type="button"
                        className="button button--link"
                        onClick={() => setEditing({ mode: 'edit', pledge })}
                        aria-label={`Edit the ${describePledge(pledge)}`}
                      >
                        Edit
                      </button>
                      <button
                        type="button"
                        className="button button--link danger"
                        onClick={() => setDeleting(pledge)}
                        aria-label={`Delete the ${describePledge(pledge)}`}
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
        <Modal title={editing.mode === 'edit' ? 'Edit pledge' : 'New pledge'} onClose={() => setEditing(undefined)}>
          <PledgeForm
            pledge={editing.mode === 'edit' ? editing.pledge : undefined}
            defaults={parent.kind === 'pledger' ? { pledgerId: parent.id } : { pledgeCategoryId: parent.id }}
            referenceData={referenceData}
            onSubmit={(request) =>
              editing.mode === 'edit' ? api.pledges.update(editing.pledge.id, request) : api.pledges.create(request)
            }
            onSaved={handleSaved}
            onCancel={() => setEditing(undefined)}
          />
        </Modal>
      )}

      {deleting && (
        <ConfirmDeleteDialog
          title="Delete pledge"
          itemDescription={`the ${describePledge(deleting)} (${formatAmount(deleting.amount)})`}
          relationshipHint="A pledge with recorded payments cannot be deleted. Delete its payments first."
          onDelete={() => api.pledges.remove(deleting.id)}
          onDeleted={() => handleDeleted(deleting)}
          onCancel={() => setDeleting(undefined)}
        />
      )}
    </div>
  )
}

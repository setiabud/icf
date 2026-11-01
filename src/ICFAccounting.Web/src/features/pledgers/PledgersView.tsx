import { useState } from 'react'
import { useApi } from '../../api/ApiContext'
import type { Pledger, SortOrder } from '../../api/types'
import { ErrorAlert } from '../../components/Alert'
import { ConfirmDeleteDialog } from '../../components/ConfirmDeleteDialog'
import { Modal } from '../../components/Modal'
import { useNotify } from '../../components/Notifications'
import { Pagination } from '../../components/Pagination'
import { EmptyState, LoadingState, StatusBadge } from '../../components/States'
import { useAsyncData } from '../../hooks/useAsyncData'
import { PledgerForm } from './PledgerForm'

const PAGE_SIZE = 10

type Editing = { mode: 'create' } | { mode: 'edit'; pledger: Pledger }

interface PledgersViewProps {
  onViewPledges: (pledgerId: string) => void
}

export function PledgersView({ onViewPledges }: PledgersViewProps) {
  const api = useApi()
  const notify = useNotify()
  const [page, setPage] = useState(1)
  const [sortOrder, setSortOrder] = useState<SortOrder>('Ascending')
  const [editing, setEditing] = useState<Editing>()
  const [deleting, setDeleting] = useState<Pledger>()

  const { state, reload } = useAsyncData(
    (signal) => api.pledgers.list({ page, pageSize: PAGE_SIZE, sortBy: 'PledgerName', sortOrder }, signal),
    [api, page, sortOrder],
  )
  const result = state.data

  function handleSaved(pledger: Pledger) {
    notify(editing?.mode === 'edit' ? `Saved ${pledger.pledgerName}.` : `Created ${pledger.pledgerName}.`)
    setEditing(undefined)
    reload()
  }

  function handleDeleted(pledger: Pledger) {
    notify(`Deleted ${pledger.pledgerName}.`)
    setDeleting(undefined)
    if (result && result.items.length === 1 && page > 1) {
      setPage(page - 1)
    } else {
      reload()
    }
  }

  return (
    <section aria-labelledby="pledgers-heading">
      <div className="section-header">
        <h2 id="pledgers-heading">Pledgers</h2>
        <div className="toolbar">
          <label className="inline-field">
            Sort
            <select
              value={sortOrder}
              onChange={(event) => {
                setSortOrder(event.target.value as SortOrder)
                setPage(1)
              }}
            >
              <option value="Ascending">Name A–Z</option>
              <option value="Descending">Name Z–A</option>
            </select>
          </label>
          <button type="button" className="button button--primary" onClick={() => setEditing({ mode: 'create' })}>
            New pledger
          </button>
        </div>
      </div>

      {state.status === 'error' && <ErrorAlert error={state.error} action="load pledgers" onRetry={reload} />}
      {state.status === 'loading' && !result && <LoadingState label="Loading pledgers…" />}
      {result && result.items.length === 0 && (
        <EmptyState>
          <p>No pledgers yet. Create the first pledger to start recording pledges.</p>
        </EmptyState>
      )}
      {result && result.items.length > 0 && (
        <>
          <div className="table-wrap" aria-busy={state.status === 'loading'}>
            <table>
              <thead>
                <tr>
                  <th scope="col">Name</th>
                  <th scope="col">Status</th>
                  <th scope="col">
                    <span className="visually-hidden">Actions</span>
                  </th>
                </tr>
              </thead>
              <tbody>
                {result.items.map((pledger) => (
                  <tr key={pledger.id}>
                    <td data-label="Name">{pledger.pledgerName}</td>
                    <td data-label="Status">
                      <StatusBadge active={pledger.active} />
                    </td>
                    <td className="row-actions">
                      <button
                        type="button"
                        className="button button--link"
                        onClick={() => onViewPledges(pledger.id)}
                        aria-label={`Pledges for ${pledger.pledgerName}`}
                      >
                        Pledges
                      </button>
                      <button
                        type="button"
                        className="button button--link"
                        onClick={() => setEditing({ mode: 'edit', pledger })}
                        aria-label={`Edit ${pledger.pledgerName}`}
                      >
                        Edit
                      </button>
                      <button
                        type="button"
                        className="button button--link danger"
                        onClick={() => setDeleting(pledger)}
                        aria-label={`Delete ${pledger.pledgerName}`}
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
        <Modal title={editing.mode === 'edit' ? 'Edit pledger' : 'New pledger'} onClose={() => setEditing(undefined)}>
          <PledgerForm
            pledger={editing.mode === 'edit' ? editing.pledger : undefined}
            onSubmit={(values) =>
              editing.mode === 'edit'
                ? api.pledgers.update(editing.pledger.id, values)
                : api.pledgers.create({ pledgerName: values.pledgerName })
            }
            onSaved={handleSaved}
            onCancel={() => setEditing(undefined)}
          />
        </Modal>
      )}

      {deleting && (
        <ConfirmDeleteDialog
          title="Delete pledger"
          itemDescription={deleting.pledgerName}
          relationshipHint="A pledger with recorded pledges cannot be deleted. Consider marking the pledger inactive instead."
          onDelete={() => api.pledgers.remove(deleting.id)}
          onDeleted={() => handleDeleted(deleting)}
          onCancel={() => setDeleting(undefined)}
        />
      )}
    </section>
  )
}

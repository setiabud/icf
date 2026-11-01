import { useState } from 'react'
import { useApi } from '../../api/ApiContext'
import type { PledgeCategory } from '../../api/types'
import { ErrorAlert } from '../../components/Alert'
import { ConfirmDeleteDialog } from '../../components/ConfirmDeleteDialog'
import { Modal } from '../../components/Modal'
import { useNotify } from '../../components/Notifications'
import { EmptyState, LoadingState, StatusBadge } from '../../components/States'
import { useAsyncData } from '../../hooks/useAsyncData'
import { CategoryForm } from './CategoryForm'

type Editing = { mode: 'create' } | { mode: 'edit'; category: PledgeCategory }

interface CategoriesViewProps {
  onViewPledges: (categoryId: string) => void
}

export function CategoriesView({ onViewPledges }: CategoriesViewProps) {
  const api = useApi()
  const notify = useNotify()
  const [editing, setEditing] = useState<Editing>()
  const [deleting, setDeleting] = useState<PledgeCategory>()
  const { state, reload } = useAsyncData((signal) => api.pledgeCategories.list(signal), [api])
  const categories = state.data

  function handleSaved(category: PledgeCategory) {
    notify(editing?.mode === 'edit' ? `Saved ${category.categoryName}.` : `Created ${category.categoryName}.`)
    setEditing(undefined)
    reload()
  }

  return (
    <section aria-labelledby="categories-heading">
      <div className="section-header">
        <h2 id="categories-heading">Pledge categories</h2>
        <div className="toolbar">
          <button type="button" className="button button--primary" onClick={() => setEditing({ mode: 'create' })}>
            New category
          </button>
        </div>
      </div>

      {state.status === 'error' && <ErrorAlert error={state.error} action="load pledge categories" onRetry={reload} />}
      {state.status === 'loading' && !categories && <LoadingState label="Loading pledge categories…" />}
      {categories && categories.length === 0 && (
        <EmptyState>
          <p>No pledge categories yet. Create a category before recording pledges.</p>
        </EmptyState>
      )}
      {categories && categories.length > 0 && (
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
              {categories.map((category) => (
                <tr key={category.id}>
                  <td data-label="Name">{category.categoryName}</td>
                  <td data-label="Status">
                    <StatusBadge active={category.active} />
                  </td>
                  <td className="row-actions">
                    <button
                      type="button"
                      className="button button--link"
                      onClick={() => onViewPledges(category.id)}
                      aria-label={`Pledges in ${category.categoryName}`}
                    >
                      Pledges
                    </button>
                    <button
                      type="button"
                      className="button button--link"
                      onClick={() => setEditing({ mode: 'edit', category })}
                      aria-label={`Edit ${category.categoryName}`}
                    >
                      Edit
                    </button>
                    <button
                      type="button"
                      className="button button--link danger"
                      onClick={() => setDeleting(category)}
                      aria-label={`Delete ${category.categoryName}`}
                    >
                      Delete
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {editing && (
        <Modal title={editing.mode === 'edit' ? 'Edit category' : 'New category'} onClose={() => setEditing(undefined)}>
          <CategoryForm
            category={editing.mode === 'edit' ? editing.category : undefined}
            onSubmit={(values) =>
              editing.mode === 'edit'
                ? api.pledgeCategories.update(editing.category.id, values)
                : api.pledgeCategories.create({ categoryName: values.categoryName })
            }
            onSaved={handleSaved}
            onCancel={() => setEditing(undefined)}
          />
        </Modal>
      )}

      {deleting && (
        <ConfirmDeleteDialog
          title="Delete pledge category"
          itemDescription={deleting.categoryName}
          relationshipHint="A category used by pledges cannot be deleted. Consider marking the category inactive instead."
          onDelete={() => api.pledgeCategories.remove(deleting.id)}
          onDeleted={() => {
            notify(`Deleted ${deleting.categoryName}.`)
            setDeleting(undefined)
            reload()
          }}
          onCancel={() => setDeleting(undefined)}
        />
      )}
    </section>
  )
}

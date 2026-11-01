import { ApiError } from '../api/ApiError'
import { useSubmission } from '../hooks/useSubmission'
import { ErrorAlert } from './Alert'
import { Modal } from './Modal'

interface ConfirmDeleteDialogProps {
  title: string
  itemDescription: string
  /** Explains which related records block deletion, shown up front and when the API returns 409. */
  relationshipHint: string
  onDelete: () => Promise<void>
  onDeleted: () => void
  onCancel: () => void
}

export function ConfirmDeleteDialog({
  title,
  itemDescription,
  relationshipHint,
  onDelete,
  onDeleted,
  onCancel,
}: ConfirmDeleteDialogProps) {
  const { pending, error, submit } = useSubmission()
  const isConflict = error instanceof ApiError && error.isConflict

  async function handleConfirm() {
    const result = await submit(onDelete)
    if (result.ok) {
      onDeleted()
    }
  }

  return (
    <Modal title={title} onClose={onCancel} busy={pending}>
      <p>
        Delete <strong>{itemDescription}</strong>? This cannot be undone.
      </p>
      {!isConflict && <p className="muted">{relationshipHint}</p>}
      {error !== undefined && (
        <ErrorAlert
          error={error}
          action="delete the record"
          conflictHint={`${relationshipHint} The record was not deleted and remains unchanged.`}
        />
      )}
      <div className="form-actions">
        <button type="button" className="button button--secondary" onClick={onCancel} disabled={pending}>
          {isConflict ? 'Close' : 'Cancel'}
        </button>
        {!isConflict && (
          <button type="button" className="button button--danger" onClick={handleConfirm} disabled={pending}>
            {pending ? 'Deleting…' : 'Delete'}
          </button>
        )}
      </div>
    </Modal>
  )
}

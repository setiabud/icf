import { useState, type FormEvent } from 'react'
import type { Pledger } from '../../api/types'
import { ErrorAlert } from '../../components/Alert'
import { Field } from '../../components/Field'
import { fieldDescribedBy } from '../../lib/fieldDescribedBy'
import { useSubmission } from '../../hooks/useSubmission'
import { validateRequiredName } from '../../lib/validation'

export interface PledgerFormValues {
  pledgerName: string
  active: boolean
}

interface PledgerFormProps {
  pledger?: Pledger
  onSubmit: (values: PledgerFormValues) => Promise<Pledger>
  onSaved: (pledger: Pledger) => void
  onCancel: () => void
}

export function PledgerForm({ pledger, onSubmit, onSaved, onCancel }: PledgerFormProps) {
  const [pledgerName, setPledgerName] = useState(pledger?.pledgerName ?? '')
  const [active, setActive] = useState(pledger?.active ?? true)
  const [nameError, setNameError] = useState<string>()
  const { pending, error, submit } = useSubmission()

  async function handleSubmit(event: FormEvent) {
    event.preventDefault()
    const validationError = validateRequiredName(pledgerName, 'Pledger name')
    setNameError(validationError)
    if (validationError) {
      return
    }
    const result = await submit(() => onSubmit({ pledgerName: pledgerName.trim(), active }))
    if (result.ok) {
      onSaved(result.value)
    }
  }

  return (
    <form onSubmit={handleSubmit} noValidate>
      <Field id="pledger-name" label="Pledger name" error={nameError}>
        <input
          id="pledger-name"
          value={pledgerName}
          onChange={(event) => setPledgerName(event.target.value)}
          aria-invalid={nameError ? true : undefined}
          aria-describedby={fieldDescribedBy('pledger-name', nameError)}
          autoComplete="off"
          required
        />
      </Field>
      {pledger && (
        <label className="checkbox">
          <input type="checkbox" checked={active} onChange={(event) => setActive(event.target.checked)} />
          Active
        </label>
      )}
      {error !== undefined && <ErrorAlert error={error} action="save the pledger" />}
      <div className="form-actions">
        <button type="button" className="button button--secondary" onClick={onCancel} disabled={pending}>
          Cancel
        </button>
        <button type="submit" className="button button--primary" disabled={pending}>
          {pending ? 'Saving…' : pledger ? 'Save changes' : 'Create pledger'}
        </button>
      </div>
    </form>
  )
}

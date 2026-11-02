import { useState, type FormEvent } from 'react'
import type { Pledger } from '../../api/types'
import { ErrorAlert } from '../../components/Alert'
import { Field } from '../../components/Field'
import { fieldDescribedBy } from '../../lib/fieldDescribedBy'
import { useSubmission } from '../../hooks/useSubmission'
import { validateOptionalEmail, validateRequiredName } from '../../lib/validation'

export interface PledgerFormValues {
  pledgerName: string
  active: boolean
  email: string | null
  enableNotification: boolean
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
  const [email, setEmail] = useState(pledger?.email ?? '')
  const [enableNotification, setEnableNotification] = useState(pledger?.enableNotification ?? false)
  const [nameError, setNameError] = useState<string>()
  const [emailError, setEmailError] = useState<string>()
  const { pending, error, submit } = useSubmission()

  async function handleSubmit(event: FormEvent) {
    event.preventDefault()
    const validationError = validateRequiredName(pledgerName, 'Pledger name')
    const emailValidationError = validateOptionalEmail(email)
    setNameError(validationError)
    setEmailError(emailValidationError)
    if (validationError || emailValidationError) {
      return
    }
    const result = await submit(() =>
      onSubmit({
        pledgerName: pledgerName.trim(),
        active,
        email: email.trim() || null,
        enableNotification,
      }),
    )
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
      <Field id="pledger-email" label="Email" error={emailError}>
        <input
          id="pledger-email"
          type="email"
          value={email}
          onChange={(event) => setEmail(event.target.value)}
          aria-invalid={emailError ? true : undefined}
          aria-describedby={fieldDescribedBy('pledger-email', emailError)}
          autoComplete="email"
        />
      </Field>
      <label className="checkbox">
        <input
          type="checkbox"
          checked={enableNotification}
          onChange={(event) => setEnableNotification(event.target.checked)}
        />
        Enable notifications
      </label>
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

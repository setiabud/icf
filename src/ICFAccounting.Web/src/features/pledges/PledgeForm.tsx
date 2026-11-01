import { useState, type FormEvent } from 'react'
import type { Guid, Pledge, SavePledgeRequest } from '../../api/types'
import { ErrorAlert } from '../../components/Alert'
import { Field } from '../../components/Field'
import { fieldDescribedBy } from '../../lib/fieldDescribedBy'
import { optionLabel, type ReferenceData } from '../../hooks/useReferenceData'
import { useSubmission } from '../../hooks/useSubmission'
import {
  hasErrors,
  MAX_PLEDGE_YEAR,
  MIN_PLEDGE_YEAR,
  toNullableComment,
  validateAmount,
  validatePledgeYear,
  validateRequiredSelection,
  type FieldErrors,
} from '../../lib/validation'

type PledgeField = 'year' | 'amount' | 'pledgerId' | 'pledgeCategoryId'

interface PledgeFormProps {
  pledge?: Pledge
  defaults?: { pledgerId?: Guid; pledgeCategoryId?: Guid }
  referenceData: ReferenceData
  onSubmit: (request: SavePledgeRequest) => Promise<Pledge>
  onSaved: (pledge: Pledge) => void
  onCancel: () => void
}

function defaultYear(): string {
  const year = new Date().getFullYear()
  return year >= MIN_PLEDGE_YEAR && year <= MAX_PLEDGE_YEAR ? String(year) : ''
}

export function PledgeForm({ pledge, defaults, referenceData, onSubmit, onSaved, onCancel }: PledgeFormProps) {
  const [year, setYear] = useState(pledge ? String(pledge.year) : defaultYear())
  const [amount, setAmount] = useState(pledge ? pledge.amount.toFixed(2) : '')
  const [pledgerId, setPledgerId] = useState(pledge?.pledgerId ?? defaults?.pledgerId ?? '')
  const [pledgeCategoryId, setPledgeCategoryId] = useState(pledge?.pledgeCategoryId ?? defaults?.pledgeCategoryId ?? '')
  const [comment, setComment] = useState(pledge?.comment ?? '')
  const [errors, setErrors] = useState<FieldErrors<PledgeField>>({})
  const { pending, error, submit } = useSubmission()

  async function handleSubmit(event: FormEvent) {
    event.preventDefault()
    const validation: FieldErrors<PledgeField> = {
      year: validatePledgeYear(year),
      amount: validateAmount(amount),
      pledgerId: validateRequiredSelection(pledgerId, 'pledger'),
      pledgeCategoryId: validateRequiredSelection(pledgeCategoryId, 'pledge category'),
    }
    setErrors(validation)
    if (hasErrors(validation)) {
      return
    }
    const request: SavePledgeRequest = {
      year: Number(year),
      amount: Number(amount),
      pledgerId,
      pledgeCategoryId,
      comment: toNullableComment(comment),
    }
    const result = await submit(() => onSubmit(request))
    if (result.ok) {
      onSaved(result.value)
    }
  }

  return (
    <form onSubmit={handleSubmit} noValidate>
      <div className="form-grid">
        <Field id="pledge-year" label="Year" error={errors.year} hint={`${MIN_PLEDGE_YEAR}–${MAX_PLEDGE_YEAR}`}>
          <input
            id="pledge-year"
            type="number"
            inputMode="numeric"
            min={MIN_PLEDGE_YEAR}
            max={MAX_PLEDGE_YEAR}
            step={1}
            value={year}
            onChange={(event) => setYear(event.target.value)}
            aria-invalid={errors.year ? true : undefined}
            aria-describedby={fieldDescribedBy('pledge-year', errors.year, 'hint')}
            required
          />
        </Field>
        <Field id="pledge-amount" label="Amount" error={errors.amount}>
          <input
            id="pledge-amount"
            inputMode="decimal"
            value={amount}
            onChange={(event) => setAmount(event.target.value)}
            aria-invalid={errors.amount ? true : undefined}
            aria-describedby={fieldDescribedBy('pledge-amount', errors.amount)}
            autoComplete="off"
            required
          />
        </Field>
        <Field id="pledge-pledger" label="Pledger" error={errors.pledgerId}>
          <select
            id="pledge-pledger"
            value={pledgerId}
            onChange={(event) => setPledgerId(event.target.value)}
            aria-invalid={errors.pledgerId ? true : undefined}
            aria-describedby={fieldDescribedBy('pledge-pledger', errors.pledgerId)}
            required
          >
            <option value="">Select a pledger</option>
            {referenceData.pledgers.map((pledger) => (
              <option key={pledger.id} value={pledger.id}>
                {optionLabel(pledger.pledgerName, pledger.active)}
              </option>
            ))}
          </select>
        </Field>
        <Field id="pledge-category" label="Pledge category" error={errors.pledgeCategoryId}>
          <select
            id="pledge-category"
            value={pledgeCategoryId}
            onChange={(event) => setPledgeCategoryId(event.target.value)}
            aria-invalid={errors.pledgeCategoryId ? true : undefined}
            aria-describedby={fieldDescribedBy('pledge-category', errors.pledgeCategoryId)}
            required
          >
            <option value="">Select a category</option>
            {referenceData.categories.map((category) => (
              <option key={category.id} value={category.id}>
                {optionLabel(category.categoryName, category.active)}
              </option>
            ))}
          </select>
        </Field>
      </div>
      <Field id="pledge-comment" label="Comment (optional)">
        <textarea id="pledge-comment" rows={3} value={comment} onChange={(event) => setComment(event.target.value)} />
      </Field>
      {error !== undefined && (
        <ErrorAlert
          error={error}
          action="save the pledge"
          conflictHint="The selected pledger or pledge category may no longer exist. Refresh and try again."
        />
      )}
      <div className="form-actions">
        <button type="button" className="button button--secondary" onClick={onCancel} disabled={pending}>
          Cancel
        </button>
        <button type="submit" className="button button--primary" disabled={pending}>
          {pending ? 'Saving…' : pledge ? 'Save changes' : 'Create pledge'}
        </button>
      </div>
    </form>
  )
}

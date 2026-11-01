import { useState, type FormEvent } from 'react'
import type { Guid, PledgePayment, UpdatePledgePaymentRequest } from '../../api/types'
import { ErrorAlert } from '../../components/Alert'
import { Field } from '../../components/Field'
import { fieldDescribedBy } from '../../lib/fieldDescribedBy'
import { useSubmission } from '../../hooks/useSubmission'
import { toApiPaymentDate, toDateInputValue } from '../../lib/format'
import {
  hasErrors,
  toNullableComment,
  validateAmount,
  validatePaymentDate,
  type FieldErrors,
} from '../../lib/validation'

type PaymentField = 'amount' | 'paymentDate'

interface PaymentFormProps {
  pledgeId: Guid
  payment?: PledgePayment
  /** Receives the update shape; `active` is ignored when creating. */
  onSubmit: (request: UpdatePledgePaymentRequest) => Promise<PledgePayment>
  onSaved: (payment: PledgePayment) => void
  onCancel: () => void
}

export function PaymentForm({ pledgeId, payment, onSubmit, onSaved, onCancel }: PaymentFormProps) {
  const [amount, setAmount] = useState(payment ? payment.amount.toFixed(2) : '')
  const [paymentDate, setPaymentDate] = useState(payment ? toDateInputValue(payment.paymentDate) : '')
  const [comment, setComment] = useState(payment?.comment ?? '')
  const [active, setActive] = useState(payment?.active ?? true)
  const [errors, setErrors] = useState<FieldErrors<PaymentField>>({})
  const { pending, error, submit } = useSubmission()

  async function handleSubmit(event: FormEvent) {
    event.preventDefault()
    const validation: FieldErrors<PaymentField> = {
      amount: validateAmount(amount),
      paymentDate: validatePaymentDate(paymentDate),
    }
    setErrors(validation)
    if (hasErrors(validation)) {
      return
    }
    const request: UpdatePledgePaymentRequest = {
      pledgeId,
      amount: Number(amount),
      paymentDate: toApiPaymentDate(paymentDate, payment?.paymentDate),
      comment: toNullableComment(comment),
      active,
    }
    const result = await submit(() => onSubmit(request))
    if (result.ok) {
      onSaved(result.value)
    }
  }

  return (
    <form onSubmit={handleSubmit} noValidate>
      <div className="form-grid">
        <Field id="payment-amount" label="Amount" error={errors.amount}>
          <input
            id="payment-amount"
            inputMode="decimal"
            value={amount}
            onChange={(event) => setAmount(event.target.value)}
            aria-invalid={errors.amount ? true : undefined}
            aria-describedby={fieldDescribedBy('payment-amount', errors.amount)}
            autoComplete="off"
            required
          />
        </Field>
        <Field id="payment-date" label="Payment date" error={errors.paymentDate}>
          <input
            id="payment-date"
            type="date"
            value={paymentDate}
            onChange={(event) => setPaymentDate(event.target.value)}
            aria-invalid={errors.paymentDate ? true : undefined}
            aria-describedby={fieldDescribedBy('payment-date', errors.paymentDate)}
            required
          />
        </Field>
      </div>
      <Field id="payment-comment" label="Comment (optional)">
        <textarea id="payment-comment" rows={3} value={comment} onChange={(event) => setComment(event.target.value)} />
      </Field>
      {payment && (
        <label className="checkbox">
          <input type="checkbox" checked={active} onChange={(event) => setActive(event.target.checked)} />
          Active
        </label>
      )}
      {error !== undefined && (
        <ErrorAlert
          error={error}
          action="save the payment"
          conflictHint="The pledge may no longer exist. Refresh and try again."
        />
      )}
      <div className="form-actions">
        <button type="button" className="button button--secondary" onClick={onCancel} disabled={pending}>
          Cancel
        </button>
        <button type="submit" className="button button--primary" disabled={pending}>
          {pending ? 'Saving…' : payment ? 'Save changes' : 'Record payment'}
        </button>
      </div>
    </form>
  )
}

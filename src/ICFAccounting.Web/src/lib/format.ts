const amountFormatter = new Intl.NumberFormat(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })

export function formatAmount(amount: number): string {
  return amountFormatter.format(amount)
}

/** Returns the YYYY-MM-DD part of an API date-time value without time-zone conversion. */
export function toDateInputValue(paymentDate: string): string {
  return paymentDate.slice(0, 10)
}

/**
 * Builds the paymentDate sent to the API from a date input value.
 * When the date is unchanged, the original value is kept so any stored time component is preserved.
 */
export function toApiPaymentDate(dateInput: string, originalPaymentDate?: string): string {
  if (originalPaymentDate && toDateInputValue(originalPaymentDate) === dateInput) {
    return originalPaymentDate
  }
  return `${dateInput}T00:00:00`
}

/** Formats an API payment date for display using its calendar date only, never shifting it by time zone. */
export function formatPaymentDate(paymentDate: string): string {
  const [year, month, day] = toDateInputValue(paymentDate).split('-').map(Number)
  if (!year || !month || !day) {
    return paymentDate
  }
  return new Intl.DateTimeFormat(undefined, { dateStyle: 'medium', timeZone: 'UTC' }).format(
    new Date(Date.UTC(year, month - 1, day)),
  )
}

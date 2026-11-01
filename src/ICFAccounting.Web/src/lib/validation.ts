export const MIN_PLEDGE_YEAR = 2020
export const MAX_PLEDGE_YEAR = 2099

const AMOUNT_PATTERN = /^\d+(\.\d{1,2})?$/
const DATE_PATTERN = /^\d{4}-\d{2}-\d{2}$/

export type FieldErrors<TField extends string> = Partial<Record<TField, string>>

export function validateRequiredName(value: string, label: string): string | undefined {
  return value.trim() ? undefined : `${label} is required.`
}

export function validatePledgeYear(value: string): string | undefined {
  const text = value.trim()
  if (!text) {
    return 'Year is required.'
  }
  const year = Number(text)
  if (!Number.isInteger(year) || year < MIN_PLEDGE_YEAR || year > MAX_PLEDGE_YEAR) {
    return `Year must be a whole number between ${MIN_PLEDGE_YEAR} and ${MAX_PLEDGE_YEAR}.`
  }
  return undefined
}

export function validateAmount(value: string): string | undefined {
  const text = value.trim()
  if (!text) {
    return 'Amount is required.'
  }
  if (!AMOUNT_PATTERN.test(text)) {
    return 'Amount must be a number with at most two decimal places.'
  }
  const amount = Number(text)
  if (!Number.isFinite(amount)) {
    return 'Amount must be a finite number greater than zero.'
  }
  if (amount <= 0) {
    return 'Amount must be greater than zero.'
  }
  return undefined
}

export function validatePaymentDate(value: string): string | undefined {
  if (!value) {
    return 'Payment date is required.'
  }
  if (!DATE_PATTERN.test(value)) {
    return 'Payment date must be a valid date.'
  }
  const [year, month, day] = value.split('-').map(Number)
  const date = new Date(0)
  date.setUTCHours(0, 0, 0, 0)
  date.setUTCFullYear(year, month - 1, day)
  if (
    year < 1 ||
    month < 1 ||
    month > 12 ||
    day < 1 ||
    date.getUTCFullYear() !== year ||
    date.getUTCMonth() !== month - 1 ||
    date.getUTCDate() !== day
  ) {
    return 'Payment date must be a valid date.'
  }
  return undefined
}

export function validateRequiredSelection(value: string, label: string): string | undefined {
  return value ? undefined : `Select a ${label}.`
}

export function hasErrors<TField extends string>(errors: FieldErrors<TField>): boolean {
  return Object.values(errors).some(Boolean)
}

/** Converts optional free text into the API's nullable comment shape. */
export function toNullableComment(value: string): string | null {
  const text = value.trim()
  return text ? text : null
}

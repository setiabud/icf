import { describe, expect, it } from 'vitest'
import { ApiError } from '../api/ApiError'
import { describeError } from './describeError'
import { formatPaymentDate, toApiPaymentDate, toDateInputValue } from './format'
import { toNullableComment, validateAmount, validatePaymentDate, validatePledgeYear } from './validation'

describe('validation', () => {
  it.each(['10', '10.5', '10.25', '0.01'])('accepts amount %s', (value) => {
    expect(validateAmount(value)).toBeUndefined()
  })

  it.each([
    ['', 'Amount is required.'],
    ['0', 'Amount must be greater than zero.'],
    ['0.00', 'Amount must be greater than zero.'],
    ['-5', 'Amount must be a number with at most two decimal places.'],
    ['10.123', 'Amount must be a number with at most two decimal places.'],
    ['abc', 'Amount must be a number with at most two decimal places.'],
  ])('rejects amount "%s"', (value, message) => {
    expect(validateAmount(value)).toBe(message)
  })

  it.each(['2020', '2099'])('accepts boundary year %s', (value) => {
    expect(validatePledgeYear(value)).toBeUndefined()
  })

  it.each(['2019', '2100', '2025.5', ''])('rejects year "%s"', (value) => {
    expect(validatePledgeYear(value)).toBeDefined()
  })

  it('requires a valid payment date', () => {
    expect(validatePaymentDate('')).toBe('Payment date is required.')
    expect(validatePaymentDate('2026-01-20')).toBeUndefined()
    expect(validatePaymentDate('2026-02-30')).toBe('Payment date must be a valid date.')
    expect(validatePaymentDate('0000-01-01')).toBe('Payment date must be a valid date.')
  })

  it('rejects amounts that cannot be represented as finite JSON numbers', () => {
    expect(validateAmount('9'.repeat(400))).toBe('Amount must be a finite number greater than zero.')
  })

  it('converts blank comments to null', () => {
    expect(toNullableComment('   ')).toBeNull()
    expect(toNullableComment(' Monthly ')).toBe('Monthly')
  })
})

describe('payment dates', () => {
  it('sends a new date as midnight without a time-zone suffix', () => {
    expect(toApiPaymentDate('2026-01-20')).toBe('2026-01-20T00:00:00')
  })

  it('preserves the original value when the date is unchanged', () => {
    expect(toApiPaymentDate('2026-01-20', '2026-01-20T14:30:00')).toBe('2026-01-20T14:30:00')
    expect(toApiPaymentDate('2026-01-21', '2026-01-20T14:30:00')).toBe('2026-01-21T00:00:00')
  })

  it('displays the stored calendar date without shifting by time zone', () => {
    expect(toDateInputValue('2026-01-01T00:00:00')).toBe('2026-01-01')
    const expected = new Intl.DateTimeFormat(undefined, { dateStyle: 'medium', timeZone: 'UTC' }).format(
      new Date(Date.UTC(2026, 0, 1)),
    )
    expect(formatPaymentDate('2026-01-01T00:00:00')).toBe(expected)
  })
})

describe('describeError', () => {
  it('explains connection failures with configuration guidance', () => {
    const description = describeError(new ApiError('network', 'Unable to reach'), 'load pledgers')

    expect(description.title).toBe('Cannot connect to the API')
    expect(description.message).toContain('VITE_API_BASE_URL')
  })

  it('combines a relationship conflict with the caller hint', () => {
    const error = new ApiError('http', 'Relationship conflict', 409, {
      title: 'Relationship conflict',
      detail: 'The operation conflicts with related accounting records.',
    })

    const description = describeError(error, 'delete the record', 'Delete its payments first.')

    expect(description.title).toBe('Relationship conflict')
    expect(description.message).toBe(
      'The operation conflicts with related accounting records. Delete its payments first.',
    )
  })

  it('lists validation errors from ValidationProblemDetails', () => {
    const error = new ApiError('http', 'Invalid', 400, {
      title: 'Invalid pledge',
      errors: { amount: ['Too many decimals.'] },
    })

    expect(describeError(error, 'save').details).toEqual(['Too many decimals.'])
  })

  it('uses a generic message for server errors without ProblemDetails', () => {
    const description = describeError(new ApiError('http', 'Request failed', 500), 'load pledgers')

    expect(description.title).toBe('The API encountered an error')
    expect(description.message).toBe('Could not load pledgers. Please try again later.')
  })
})

import { ApiError } from '../api/ApiError'

export interface ErrorDescription {
  title: string
  message: string
  details: string[]
}

/**
 * Turns any thrown value into a safe, user-facing description.
 * `conflictHint` explains which related records are involved when the API returns 409.
 */
export function describeError(error: unknown, action: string, conflictHint?: string): ErrorDescription {
  if (!(error instanceof ApiError)) {
    return { title: `Could not ${action}`, message: 'An unexpected error occurred. Please try again.', details: [] }
  }

  if (error.kind === 'network') {
    return {
      title: 'Cannot connect to the API',
      message:
        `${error.message} Make sure ICFAccounting.Api is running, ` +
        'VITE_API_BASE_URL points to it, and the HTTPS development certificate is trusted.',
      details: [],
    }
  }

  const problem = error.problem
  const validationDetails = Object.values(problem?.errors ?? {}).flat()

  switch (error.status) {
    case 400:
      return {
        title: problem?.title ?? `Could not ${action}`,
        message: problem?.detail ?? 'The API rejected the request. Check the values and try again.',
        details: validationDetails,
      }
    case 404:
      return {
        title: 'Record not found',
        message:
          problem?.detail ?? 'The record no longer exists. It may have been deleted. Refresh the list and try again.',
        details: [],
      }
    case 409:
      return {
        title: problem?.title ?? 'Relationship conflict',
        message: [problem?.detail ?? 'Related accounting records prevent this change.', conflictHint]
          .filter(Boolean)
          .join(' '),
        details: [],
      }
    default:
      if (error.status !== undefined && error.status >= 500) {
        return {
          title: 'The API encountered an error',
          message: `Could not ${action}. ${problem?.detail ?? 'Please try again later.'}`,
          details: [],
        }
      }
      return {
        title: problem?.title ?? `Could not ${action}`,
        message: problem?.detail ?? `The API responded with status ${error.status}.`,
        details: validationDetails,
      }
  }
}

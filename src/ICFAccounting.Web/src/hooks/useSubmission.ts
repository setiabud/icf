import { useCallback, useRef, useState } from 'react'

export type SubmissionResult<T> = { ok: true; value: T } | { ok: false }

export interface Submission {
  pending: boolean
  error: unknown
  clearError: () => void
  /** Runs the action unless one is already in flight; failures are captured in `error`. */
  submit: <T>(action: () => Promise<T>) => Promise<SubmissionResult<T>>
}

export function useSubmission(): Submission {
  const inFlight = useRef(false)
  const [pending, setPending] = useState(false)
  const [error, setError] = useState<unknown>(undefined)

  const submit = useCallback(async <T>(action: () => Promise<T>): Promise<SubmissionResult<T>> => {
    if (inFlight.current) {
      return { ok: false }
    }
    inFlight.current = true
    setPending(true)
    setError(undefined)
    try {
      return { ok: true, value: await action() }
    } catch (caught) {
      setError(caught)
      return { ok: false }
    } finally {
      inFlight.current = false
      setPending(false)
    }
  }, [])

  const clearError = useCallback(() => setError(undefined), [])
  return { pending, error, clearError, submit }
}

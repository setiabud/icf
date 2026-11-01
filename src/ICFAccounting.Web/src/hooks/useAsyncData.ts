import { useCallback, useEffect, useState, type DependencyList } from 'react'
import { isAbortError } from '../api/ApiError'

export type AsyncState<T> =
  | { status: 'loading'; data: T | undefined; error: undefined }
  | { status: 'success'; data: T; error: undefined }
  | { status: 'error'; data: T | undefined; error: unknown }

export interface AsyncResource<T> {
  state: AsyncState<T>
  reload: () => void
}

/**
 * Loads data whenever the dependencies change. Earlier requests are aborted and their
 * results ignored, so switching between parent records never shows stale data.
 */
export function useAsyncData<T>(load: (signal: AbortSignal) => Promise<T>, deps: DependencyList): AsyncResource<T> {
  const [state, setState] = useState<AsyncState<T>>({ status: 'loading', data: undefined, error: undefined })
  const [reloadToken, setReloadToken] = useState(0)

  useEffect(() => {
    const controller = new AbortController()
    setState((previous) => ({ status: 'loading', data: previous.data, error: undefined }))

    load(controller.signal).then(
      (data) => {
        if (!controller.signal.aborted) {
          setState({ status: 'success', data, error: undefined })
        }
      },
      (error: unknown) => {
        if (controller.signal.aborted || isAbortError(error)) {
          return
        }
        setState((previous) => ({ status: 'error', data: previous.data, error }))
      },
    )

    return () => controller.abort()
    // eslint-disable-next-line react-hooks/exhaustive-deps -- callers declare the dependencies of `load`.
  }, [...deps, reloadToken])

  const reload = useCallback(() => setReloadToken((token) => token + 1), [])
  return { state, reload }
}

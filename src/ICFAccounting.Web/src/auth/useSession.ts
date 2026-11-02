import { useCallback, useEffect, useRef, useState } from 'react'
import { ApiError, isAbortError } from '../api/ApiError'
import { SESSION_EXPIRED_EVENT } from '../api/httpClient'
import { sessionApi, type Session } from './sessionApi'

type SessionState =
  | { status: 'loading' }
  | { status: 'anonymous' }
  | { status: 'authenticated'; user: Session }
  | { status: 'error'; error: unknown }

export function useSession() {
  const [state, setState] = useState<SessionState>({ status: 'loading' })
  const [message, setMessage] = useState('')
  const [logoutError, setLogoutError] = useState<unknown>()
  const [loggingOut, setLoggingOut] = useState(false)
  const request = useRef<AbortController | null>(null)
  const busy = useRef(false)

  const expire = useCallback(() => {
    request.current?.abort()
    setState({ status: 'anonymous' })
    setMessage('Your session expired. Sign in again to continue where you left off.')
  }, [])

  const restore = useCallback(() => {
    if (busy.current) return
    request.current?.abort()
    const controller = new AbortController()
    request.current = controller
    void sessionApi.restore(controller.signal).then((user) => {
      if (!controller.signal.aborted) {
        setState({ status: 'authenticated', user })
      }
    }, (error: unknown) => {
      if (controller.signal.aborted || isAbortError(error)) return
      if (error instanceof ApiError && error.status === 401) {
        setState({ status: 'anonymous' })
        setMessage('Sign in to continue. If your session expired, your intended page has been preserved.')
      } else {
        setState({ status: 'error', error })
      }
    })
  }, [])

  useEffect(() => {
    void restore()
    window.addEventListener(SESSION_EXPIRED_EVENT, expire)
    return () => {
      request.current?.abort()
      window.removeEventListener(SESSION_EXPIRED_EVENT, expire)
    }
  }, [restore, expire])

  useEffect(() => {
    if (state.status !== 'authenticated') return
    const delay = new Date(state.user.expiresAt).getTime() - Date.now()
    const timer = window.setTimeout(expire, Math.max(0, delay))
    const checkSession = () => { void restore() }
    window.addEventListener('focus', checkSession)
    return () => {
      window.clearTimeout(timer)
      window.removeEventListener('focus', checkSession)
    }
  }, [state, expire, restore])

  async function login(email: string, password: string, signal: AbortSignal) {
    if (busy.current) {
      throw new ApiError('http', 'Another authentication request is still in progress. Please try again.')
    }
    request.current?.abort()
    busy.current = true
    try {
      const user = await sessionApi.login(email, password, signal)
      if (!signal.aborted) {
        setMessage('')
        setLogoutError(undefined)
        setState({ status: 'authenticated', user })
      }
    } finally {
      busy.current = false
    }
  }

  async function logout() {
    if (busy.current) return
    busy.current = true
    request.current?.abort()
    setLoggingOut(true)
    setLogoutError(undefined)
    try {
      await sessionApi.logout()
      setState({ status: 'anonymous' })
      setMessage('You have signed out.')
    } catch (error) {
      if (error instanceof ApiError && error.status === 401) {
        expire()
      } else {
        setLogoutError(error)
      }
    } finally {
      busy.current = false
      setLoggingOut(false)
    }
  }

  return { state, message, login, logout, restore, logoutError, loggingOut }
}

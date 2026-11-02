import { useEffect, useRef, useState, type FormEvent } from 'react'
import { ApiError, isAbortError } from '../api/ApiError'
import { Alert } from '../components/Alert'
import { Field } from '../components/Field'

interface LoginFormProps {
  onLogin: (email: string, password: string, signal: AbortSignal) => Promise<void>
  message: string
}

export function LoginForm({ onLogin, message }: LoginFormProps) {
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [pending, setPending] = useState(false)
  const [error, setError] = useState('')
  const [cancelled, setCancelled] = useState(false)
  const request = useRef<AbortController | null>(null)
  const passwordInput = useRef<HTMLInputElement>(null)

  useEffect(() => () => request.current?.abort(), [])
  useEffect(() => {
    if (error && !pending) passwordInput.current?.focus()
  }, [error, pending])

  async function submit(event: FormEvent) {
    event.preventDefault()
    if (request.current) return
    const controller = new AbortController()
    request.current = controller
    setPending(true)
    setError('')
    setCancelled(false)
    try {
      await onLogin(email.trim(), password, controller.signal)
    } catch (failure) {
      if (controller.signal.aborted || isAbortError(failure)) {
        setCancelled(true)
      } else {
        setError(failure instanceof ApiError
          ? [failure.message, failure.problem?.detail].filter(Boolean).join('. ')
          : 'Sign-in failed. Please try again.')
      }
    } finally {
      setPassword('')
      setPending(false)
      request.current = null
    }
  }

  return (
    <section className="login-panel" aria-labelledby="login-heading">
      <h2 id="login-heading">Sign in</h2>
      <p>Use the account provided by your administrator. Public registration is not available.</p>
      {message && <Alert tone="info" title={message} />}
      {cancelled && <Alert tone="info" title="Sign-in cancelled. You can try again." />}
      {error && <Alert tone="error" title={error} />}
      <form onSubmit={submit} aria-busy={pending}>
        <Field id="login-email" label="Email">
          <input id="login-email" type="email" autoComplete="username" required maxLength={256}
            value={email} onChange={(event) => setEmail(event.target.value)} disabled={pending} />
        </Field>
        <Field id="login-password" label="Password">
          <input ref={passwordInput} id="login-password" type="password" autoComplete="current-password"
            required maxLength={1024} value={password}
            onChange={(event) => setPassword(event.target.value)} disabled={pending} />
        </Field>
        <div className="form-actions">
          {pending && <button type="button" className="button button--secondary"
            onClick={() => request.current?.abort()}>Cancel</button>}
          <button type="submit" className="button button--primary" disabled={pending}>
            {pending ? 'Signing in…' : 'Sign in'}
          </button>
        </div>
      </form>
    </section>
  )
}

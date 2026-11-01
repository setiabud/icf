import type { ReactNode } from 'react'
import { describeError } from '../lib/describeError'

interface AlertProps {
  tone: 'error' | 'success' | 'info'
  title: string
  children?: ReactNode
  action?: ReactNode
}

export function Alert({ tone, title, children, action }: AlertProps) {
  return (
    <div className={`alert alert--${tone}`} role={tone === 'error' ? 'alert' : 'status'}>
      <div className="alert__body">
        <strong className="alert__title">{title}</strong>
        {children}
      </div>
      {action}
    </div>
  )
}

interface ErrorAlertProps {
  error: unknown
  action: string
  conflictHint?: string
  onRetry?: () => void
}

export function ErrorAlert({ error, action, conflictHint, onRetry }: ErrorAlertProps) {
  const description = describeError(error, action, conflictHint)
  return (
    <Alert
      tone="error"
      title={description.title}
      action={
        onRetry && (
          <button type="button" className="button button--secondary" onClick={onRetry}>
            Retry
          </button>
        )
      }
    >
      <p>{description.message}</p>
      {description.details.length > 0 && (
        <ul>
          {description.details.map((detail) => (
            <li key={detail}>{detail}</li>
          ))}
        </ul>
      )}
    </Alert>
  )
}

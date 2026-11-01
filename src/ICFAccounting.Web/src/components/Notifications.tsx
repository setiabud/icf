import { createContext, useCallback, useContext, useEffect, useState, type ReactNode } from 'react'

type Notify = (message: string) => void

const NotificationContext = createContext<Notify>(() => {})

export function NotificationProvider({ children }: { children: ReactNode }) {
  const [message, setMessage] = useState<string>()

  useEffect(() => {
    if (!message) {
      return
    }
    const timeout = window.setTimeout(() => setMessage(undefined), 5000)
    return () => window.clearTimeout(timeout)
  }, [message])

  const notify = useCallback<Notify>((text) => setMessage(text), [])

  return (
    <NotificationContext.Provider value={notify}>
      {children}
      <div className="toast-region" role="status" aria-live="polite">
        {message && (
          <div className="toast">
            <span>{message}</span>
            <button type="button" className="button button--link" onClick={() => setMessage(undefined)}>
              Dismiss
            </button>
          </div>
        )}
      </div>
    </NotificationContext.Provider>
  )
}

// eslint-disable-next-line react-refresh/only-export-components
export function useNotify(): Notify {
  return useContext(NotificationContext)
}

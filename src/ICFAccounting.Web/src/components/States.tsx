import type { ReactNode } from 'react'

export function StatusBadge({ active }: { active: boolean }) {
  return (
    <span className={`badge ${active ? 'badge--active' : 'badge--inactive'}`}>{active ? 'Active' : 'Inactive'}</span>
  )
}

export function LoadingState({ label }: { label: string }) {
  return (
    <p className="state state--loading" role="status">
      {label}
    </p>
  )
}

export function EmptyState({ children }: { children: ReactNode }) {
  return <div className="state state--empty">{children}</div>
}

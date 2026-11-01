/** Builds the aria-describedby value matching the hint and error ids rendered by `Field`. */
export function fieldDescribedBy(id: string, error?: string, hint?: string): string | undefined {
  const ids = [hint ? `${id}-hint` : undefined, error ? `${id}-error` : undefined].filter(Boolean)
  return ids.length > 0 ? ids.join(' ') : undefined
}

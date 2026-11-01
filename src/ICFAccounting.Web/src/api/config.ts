export const DEFAULT_API_BASE_URL = 'http://localhost:5101'

export function resolveApiBaseUrl(configured: string | undefined): string {
  const value = configured?.trim()
  return (value ? value : DEFAULT_API_BASE_URL).replace(/\/+$/, '')
}

export const apiBaseUrl = resolveApiBaseUrl(import.meta.env.VITE_API_BASE_URL)

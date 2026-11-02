import { httpClient } from '../api/httpClient'

export interface Session {
  id: string
  name: string
  email: string
  profileImageUrl: string | null
  expiresAt: string
}

export const sessionApi = {
  restore: (signal: AbortSignal) => httpClient.get<Session>('/auth/session', { signal }),
  login: (email: string, password: string, signal: AbortSignal) =>
    httpClient.post<Session>('/auth/login', { body: { email, password }, signal }),
  logout: () => httpClient.post<void>('/auth/logout'),
}

import { act, fireEvent, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import App from './App'
import { ApiError } from './api/ApiError'
import { SESSION_EXPIRED_EVENT } from './api/httpClient'
import { sessionApi, type Session } from './auth/sessionApi'
import { createFakeApi, deferred, page, renderWithApi } from './test/testUtils'

vi.mock('./auth/sessionApi', () => ({
  sessionApi: { restore: vi.fn(), login: vi.fn(), logout: vi.fn() },
}))

const profile = (): Session => ({
  id: 'account-1', name: 'Test Member', email: 'member@example.test',
  profileImageUrl: 'https://images.example.test/avatar.png',
  expiresAt: new Date(Date.now() + 8 * 60 * 60 * 1000).toISOString(),
})

function setup() {
  const api = createFakeApi()
  api.pledgeCategories.list.mockResolvedValue([])
  api.pledgers.list.mockResolvedValue(page([]))
  renderWithApi(<App />, api)
  return api
}

beforeEach(() => {
  vi.resetAllMocks()
  window.history.replaceState(null, '', '#/categories')
})
afterEach(() => vi.useRealTimers())

describe('authenticated app', () => {
  it('restores returning users and shows their name/image without requesting a password', async () => {
    vi.mocked(sessionApi.restore).mockResolvedValue(profile())
    setup()
    expect(await screen.findByText('Test Member')).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: 'Pledge categories' })).toBeInTheDocument()
    expect(screen.queryByLabelText('Password')).not.toBeInTheDocument()
    expect(document.querySelector('.account-avatar')).toHaveAttribute('src', profile().profileImageUrl)
  })

  it('blocks protected content until login and returns to the intended page after signing in', async () => {
    vi.mocked(sessionApi.restore).mockRejectedValue(new ApiError('http', 'Sign-in required', 401))
    vi.mocked(sessionApi.login).mockResolvedValue(profile())
    const api = setup()
    await screen.findByLabelText('Email')
    expect(screen.queryByRole('navigation')).not.toBeInTheDocument()
    expect(api.pledgeCategories.list).not.toHaveBeenCalled()
    const user = userEvent.setup()
    await user.type(screen.getByLabelText('Email'), 'member@example.test')
    await user.type(screen.getByLabelText('Password'), 'Test-Password-83!')
    await user.click(screen.getByRole('button', { name: 'Sign in' }))
    expect(await screen.findByText('Test Member')).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: 'Pledge categories' })).toBeInTheDocument()
    expect(window.location.hash).toBe('#/categories')
  })

  it('keeps content hidden while restoring a session and offers retry after a network failure', async () => {
    const pending = deferred<Session>()
    vi.mocked(sessionApi.restore).mockReturnValueOnce(pending.promise).mockResolvedValueOnce(profile())
    const api = setup()
    expect(screen.getByText('Restoring your session…')).toHaveAttribute('role', 'status')
    expect(api.pledgeCategories.list).not.toHaveBeenCalled()
    await act(async () => pending.reject(new ApiError('network', 'Unable to reach the API')))
    expect(await screen.findByRole('alert')).toBeInTheDocument()
    fireEvent.click(screen.getByRole('button', { name: 'Retry' }))
    expect(await screen.findByText('Test Member')).toBeInTheDocument()
  })

  it('signs out, removes protected content, and preserves the intended page', async () => {
    vi.mocked(sessionApi.restore).mockResolvedValue(profile())
    vi.mocked(sessionApi.logout).mockResolvedValue()
    setup()
    await screen.findByText('Test Member')
    fireEvent.click(screen.getByRole('button', { name: 'Sign out' }))
    expect(await screen.findByLabelText('Password')).toBeInTheDocument()
    expect(screen.queryByRole('navigation')).not.toBeInTheDocument()
    expect(screen.getByText('You have signed out.')).toBeInTheDocument()
    expect(window.location.hash).toBe('#/categories')
  })

  it('reports a failed logout without pretending the session ended', async () => {
    vi.mocked(sessionApi.restore).mockResolvedValue(profile())
    vi.mocked(sessionApi.logout).mockRejectedValue(new ApiError('network', 'Unable to reach the API'))
    setup()
    await screen.findByText('Test Member')
    fireEvent.click(screen.getByRole('button', { name: 'Sign out' }))
    expect(await screen.findByRole('alert')).toBeInTheDocument()
    expect(screen.getByText('Test Member')).toBeInTheDocument()
  })

  it('does not race a new login against an in-flight logout after expiration', async () => {
    const pending = deferred<void>()
    vi.mocked(sessionApi.restore).mockResolvedValue(profile())
    vi.mocked(sessionApi.logout).mockReturnValue(pending.promise)
    setup()
    await screen.findByText('Test Member')
    fireEvent.click(screen.getByRole('button', { name: 'Sign out' }))
    act(() => window.dispatchEvent(new Event(SESSION_EXPIRED_EVENT)))
    fireEvent.change(screen.getByLabelText('Email'), { target: { value: 'member@example.test' } })
    fireEvent.change(screen.getByLabelText('Password'), { target: { value: 'Test-Password-83!' } })
    fireEvent.submit(screen.getByRole('button', { name: 'Sign in' }).closest('form')!)
    expect(await screen.findByRole('alert')).toHaveTextContent('Another authentication request')
    expect(sessionApi.login).not.toHaveBeenCalled()
    await act(async () => pending.resolve())
  })

  it('removes protected content on an API unauthorized response and allows returning-user login', async () => {
    vi.mocked(sessionApi.restore).mockResolvedValue(profile())
    vi.mocked(sessionApi.login).mockResolvedValue(profile())
    setup()
    await screen.findByText('Test Member')
    act(() => window.dispatchEvent(new Event(SESSION_EXPIRED_EVENT)))
    expect(await screen.findByLabelText('Password')).toBeInTheDocument()
    expect(screen.getByText('Your session expired. Sign in again to continue where you left off.')).toBeInTheDocument()
    expect(screen.queryByText('Test Member')).not.toBeInTheDocument()
    fireEvent.change(screen.getByLabelText('Email'), { target: { value: 'member@example.test' } })
    fireEvent.change(screen.getByLabelText('Password'), { target: { value: 'Test-Password-83!' } })
    fireEvent.submit(screen.getByRole('button', { name: 'Sign in' }).closest('form')!)
    expect(await screen.findByText('Test Member')).toBeInTheDocument()
  })

  it('expires the UI at the absolute deadline without waiting for another API call', async () => {
    vi.useFakeTimers()
    vi.mocked(sessionApi.restore).mockResolvedValue({ ...profile(), expiresAt: new Date(Date.now() + 60_000).toISOString() })
    await act(async () => { setup() })
    expect(screen.getByText('Test Member')).toBeInTheDocument()
    act(() => vi.advanceTimersByTime(60_001))
    expect(screen.getByLabelText('Password')).toBeInTheDocument()
    expect(screen.getByText('Your session expired. Sign in again to continue where you left off.')).toBeInTheDocument()
  })

  it('aborts restoration when unmounted', async () => {
    const pending = deferred<Session>()
    vi.mocked(sessionApi.restore).mockReturnValue(pending.promise)
    const api = createFakeApi()
    const result = renderWithApi(<App />, api)
    const signal = vi.mocked(sessionApi.restore).mock.calls[0]![0]
    result.unmount()
    expect(signal.aborted).toBe(true)
    await waitFor(() => expect(api.pledgers.list).not.toHaveBeenCalled())
  })
})

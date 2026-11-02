import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { ApiError } from '../api/ApiError'
import { deferred } from '../test/testUtils'
import { LoginForm } from './LoginForm'

describe('LoginForm', () => {
  it('submits email/password and clears the password after failure', async () => {
    const login = vi.fn().mockRejectedValue(new ApiError('http', 'Sign-in failed', 401))
    render(<LoginForm onLogin={login} message="" />)
    const user = userEvent.setup()
    await user.type(screen.getByLabelText('Email'), 'member@example.test')
    await user.type(screen.getByLabelText('Password'), 'incorrect-password')
    await user.click(screen.getByRole('button', { name: 'Sign in' }))
    await screen.findByRole('alert')
    expect(login).toHaveBeenCalledWith('member@example.test', 'incorrect-password', expect.any(AbortSignal))
    expect(screen.getByLabelText('Password')).toHaveValue('')
    expect(screen.getByRole('alert')).toHaveTextContent('Sign-in failed')
  })

  it('supports cancellation without reporting success or allowing duplicate requests', async () => {
    const pending = deferred<void>()
    const login = vi.fn().mockImplementation((_email, _password, signal: AbortSignal) => {
      signal.addEventListener('abort', () => pending.reject(new DOMException('Aborted', 'AbortError')))
      return pending.promise
    })
    render(<LoginForm onLogin={login} message="" />)
    fireEvent.change(screen.getByLabelText('Email'), { target: { value: 'member@example.test' } })
    fireEvent.change(screen.getByLabelText('Password'), { target: { value: 'secret' } })
    fireEvent.submit(screen.getByRole('button', { name: 'Sign in' }).closest('form')!)
    expect(screen.getByRole('button', { name: 'Signing in…' })).toBeDisabled()
    fireEvent.click(screen.getByRole('button', { name: 'Cancel' }))
    await waitFor(() => expect(screen.getByRole('status')).toHaveTextContent('Sign-in cancelled'))
    expect(screen.getByLabelText('Password')).toHaveValue('')
    expect(login).toHaveBeenCalledTimes(1)
  })
})

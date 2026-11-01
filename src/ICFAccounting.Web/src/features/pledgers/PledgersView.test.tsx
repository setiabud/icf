import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { ApiError } from '../../api/ApiError'
import { createFakeApi, deferred, page, pledger, renderWithApi } from '../../test/testUtils'
import { PledgersView } from './PledgersView'

describe('PledgersView', () => {
  it('lists pledgers from the API with default paging and sorting', async () => {
    const api = createFakeApi()
    api.pledgers.list.mockResolvedValue(page([pledger('p1', 'Ada Lovelace'), pledger('p2', 'Grace Hopper', false)]))

    renderWithApi(<PledgersView onViewPledges={vi.fn()} />, api)

    expect(await screen.findByText('Ada Lovelace')).toBeInTheDocument()
    expect(screen.getByText('Inactive')).toBeInTheDocument()
    expect(api.pledgers.list).toHaveBeenCalledWith(
      { page: 1, pageSize: 10, sortBy: 'PledgerName', sortOrder: 'Ascending' },
      expect.any(AbortSignal),
    )
  })

  it('shows an empty state when there are no pledgers', async () => {
    const api = createFakeApi()
    api.pledgers.list.mockResolvedValue(page([]))

    renderWithApi(<PledgersView onViewPledges={vi.fn()} />, api)

    expect(await screen.findByText(/No pledgers yet/)).toBeInTheDocument()
  })

  it('shows an actionable connection error and retries', async () => {
    const api = createFakeApi()
    api.pledgers.list
      .mockRejectedValueOnce(new ApiError('network', 'Unable to reach the API.'))
      .mockResolvedValueOnce(page([pledger('p1', 'Ada Lovelace')]))
    const user = userEvent.setup()

    renderWithApi(<PledgersView onViewPledges={vi.fn()} />, api)

    expect(await screen.findByText('Cannot connect to the API')).toBeInTheDocument()
    await user.click(screen.getByRole('button', { name: 'Retry' }))
    expect(await screen.findByText('Ada Lovelace')).toBeInTheDocument()
  })

  it('validates the name before creating and prevents duplicate submissions', async () => {
    const api = createFakeApi()
    api.pledgers.list.mockResolvedValue(page([]))
    const creation = deferred<ReturnType<typeof pledger>>()
    api.pledgers.create.mockReturnValue(creation.promise)
    const user = userEvent.setup()

    renderWithApi(<PledgersView onViewPledges={vi.fn()} />, api)
    await screen.findByText(/No pledgers yet/)
    await user.click(screen.getByRole('button', { name: 'New pledger' }))
    const dialog = screen.getByRole('dialog', { name: 'New pledger' })

    await user.click(screen.getByRole('button', { name: 'Create pledger' }))
    expect(screen.getByText('Pledger name is required.')).toBeInTheDocument()
    expect(api.pledgers.create).not.toHaveBeenCalled()

    await user.type(screen.getByLabelText('Pledger name'), '  Ada Lovelace ')
    await user.click(screen.getByRole('button', { name: 'Create pledger' }))
    const savingButton = screen.getByRole('button', { name: 'Saving…' })
    expect(savingButton).toBeDisabled()
    await user.click(savingButton)

    expect(api.pledgers.create).toHaveBeenCalledTimes(1)
    expect(api.pledgers.create).toHaveBeenCalledWith({ pledgerName: 'Ada Lovelace' })

    creation.resolve(pledger('p1', 'Ada Lovelace'))
    await waitFor(() => expect(dialog).not.toBeInTheDocument())
    expect(await screen.findByText('Created Ada Lovelace.')).toBeInTheDocument()
  })

  it('keeps the pledger and explains the conflict when deletion is rejected', async () => {
    const api = createFakeApi()
    api.pledgers.list.mockResolvedValue(page([pledger('p1', 'Ada Lovelace')]))
    api.pledgers.remove.mockRejectedValue(
      new ApiError('http', 'Relationship conflict', 409, {
        title: 'Relationship conflict',
        detail: 'The operation conflicts with related accounting records.',
      }),
    )
    const user = userEvent.setup()

    renderWithApi(<PledgersView onViewPledges={vi.fn()} />, api)
    await user.click(await screen.findByRole('button', { name: 'Delete Ada Lovelace' }))
    await user.click(screen.getByRole('button', { name: 'Delete' }))

    const alert = await screen.findByRole('alert')
    expect(alert).toHaveTextContent('Relationship conflict')
    expect(alert).toHaveTextContent('A pledger with recorded pledges cannot be deleted.')
    expect(alert).toHaveTextContent('The record was not deleted and remains unchanged.')
    expect(screen.queryByRole('button', { name: 'Delete' })).not.toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: 'Close' }))
    expect(screen.getByText('Ada Lovelace')).toBeInTheDocument()
    expect(api.pledgers.list).toHaveBeenCalledTimes(1)
  })

  it('requires confirmation before deleting and reloads after success', async () => {
    const api = createFakeApi()
    api.pledgers.list.mockResolvedValueOnce(page([pledger('p1', 'Ada Lovelace')])).mockResolvedValueOnce(page([]))
    api.pledgers.remove.mockResolvedValue(undefined)
    const user = userEvent.setup()

    renderWithApi(<PledgersView onViewPledges={vi.fn()} />, api)
    await user.click(await screen.findByRole('button', { name: 'Delete Ada Lovelace' }))
    expect(api.pledgers.remove).not.toHaveBeenCalled()
    await user.click(screen.getByRole('button', { name: 'Delete' }))

    expect(api.pledgers.remove).toHaveBeenCalledWith('p1')
    expect(await screen.findByText(/No pledgers yet/)).toBeInTheDocument()
  })
})

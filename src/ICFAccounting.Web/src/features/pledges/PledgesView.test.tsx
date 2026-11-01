import { screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { ApiError } from '../../api/ApiError'
import type { PagedResponse, Pledge } from '../../api/types'
import { category, createFakeApi, deferred, page, pledge, pledger, renderWithApi } from '../../test/testUtils'
import { PledgesView } from './PledgesView'

function setupReferenceData() {
  const api = createFakeApi()
  api.pledgers.listAll.mockResolvedValue([pledger('pledger-1', 'Ada Lovelace'), pledger('pledger-2', 'Grace Hopper')])
  api.pledgeCategories.list.mockResolvedValue([
    category('category-1', 'General Fund'),
    category('category-2', 'Missions'),
  ])
  return api
}

describe('PledgesView', () => {
  it('ignores a slow response for a previously selected pledger', async () => {
    const api = setupReferenceData()
    const first = deferred<PagedResponse<Pledge>>()
    const second = deferred<PagedResponse<Pledge>>()
    api.pledges.listByPledger.mockReturnValueOnce(first.promise).mockReturnValueOnce(second.promise)

    const { rerender } = renderWithApi(
      <PledgesView parent={{ kind: 'pledger', id: 'pledger-1' }} onParentChange={vi.fn()} onViewPayments={vi.fn()} />,
      api,
    )
    await screen.findByRole('heading', { name: 'Ada Lovelace' })
    const firstSignal = api.pledges.listByPledger.mock.calls[0]![2] as AbortSignal

    rerender(
      <PledgesView parent={{ kind: 'pledger', id: 'pledger-2' }} onParentChange={vi.fn()} onViewPayments={vi.fn()} />,
    )
    await screen.findByRole('heading', { name: 'Grace Hopper' })
    expect(firstSignal.aborted).toBe(true)

    second.resolve(page([pledge('b', { pledgerId: 'pledger-2', year: 2027, amount: 300 })]))
    first.resolve(page([pledge('a', { pledgerId: 'pledger-1', year: 2024, amount: 999 })]))

    expect(await screen.findByText('2027')).toBeInTheDocument()
    expect(screen.queryByText('2024')).not.toBeInTheDocument()
  })

  it('creates a pledge for the selected category with validated, typed values', async () => {
    const api = setupReferenceData()
    api.pledges.listByCategory.mockResolvedValue(page([]))
    api.pledges.create.mockResolvedValue(pledge('new', { pledgeCategoryId: 'category-2' }))
    const user = userEvent.setup()

    renderWithApi(
      <PledgesView parent={{ kind: 'category', id: 'category-2' }} onParentChange={vi.fn()} onViewPayments={vi.fn()} />,
      api,
    )
    await user.click(await screen.findByRole('button', { name: 'New pledge' }))
    const dialog = screen.getByRole('dialog', { name: 'New pledge' })
    const form = within(dialog)

    await user.clear(form.getByLabelText('Year'))
    await user.type(form.getByLabelText('Year'), '2019')
    await user.type(form.getByLabelText('Amount'), '10.555')
    await user.click(form.getByRole('button', { name: 'Create pledge' }))
    expect(form.getByText('Year must be a whole number between 2020 and 2099.')).toBeInTheDocument()
    expect(form.getByText('Amount must be a number with at most two decimal places.')).toBeInTheDocument()
    expect(form.getByText('Select a pledger.')).toBeInTheDocument()
    expect(api.pledges.create).not.toHaveBeenCalled()

    await user.clear(form.getByLabelText('Year'))
    await user.type(form.getByLabelText('Year'), '2026')
    await user.clear(form.getByLabelText('Amount'))
    await user.type(form.getByLabelText('Amount'), '1200.50')
    await user.selectOptions(form.getByLabelText('Pledger'), 'pledger-2')
    await user.click(form.getByRole('button', { name: 'Create pledge' }))

    expect(api.pledges.create).toHaveBeenCalledWith({
      year: 2026,
      amount: 1200.5,
      pledgerId: 'pledger-2',
      pledgeCategoryId: 'category-2',
      comment: null,
    })
  })

  it('shows API validation details and keeps the form open when the API rejects a save', async () => {
    const api = setupReferenceData()
    api.pledges.listByPledger.mockResolvedValue(page([pledge('p1')]))
    api.pledges.update.mockRejectedValue(
      new ApiError('http', 'Invalid pledge', 400, {
        title: 'Invalid pledge',
        detail: 'Year must be between 2020 and 2099.',
      }),
    )
    const user = userEvent.setup()

    renderWithApi(
      <PledgesView parent={{ kind: 'pledger', id: 'pledger-1' }} onParentChange={vi.fn()} onViewPayments={vi.fn()} />,
      api,
    )
    await user.click(await screen.findByRole('button', { name: /^Edit the 2026 General Fund pledge/ }))
    await user.click(screen.getByRole('button', { name: 'Save changes' }))

    const alert = await screen.findByRole('alert')
    expect(alert).toHaveTextContent('Invalid pledge')
    expect(alert).toHaveTextContent('Year must be between 2020 and 2099.')
    expect(screen.getByRole('dialog', { name: 'Edit pledge' })).toBeInTheDocument()
  })
})

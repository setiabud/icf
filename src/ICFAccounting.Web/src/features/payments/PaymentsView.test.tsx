import { screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import type { PledgePayment } from '../../api/types'
import { category, createFakeApi, page, pledge, pledger, renderWithApi } from '../../test/testUtils'
import { PaymentsView } from './PaymentsView'

function payment(values: Partial<PledgePayment> = {}): PledgePayment {
  return {
    id: 'payment-1',
    pledgeId: 'pledge-1',
    amount: 50,
    paymentDate: '2026-01-20T00:00:00',
    comment: 'Monthly payment',
    active: true,
    createdAt: '2026-01-20T12:00:00Z',
    updatedAt: '2026-01-20T12:00:00Z',
    ...values,
  }
}

function setup() {
  const api = createFakeApi()
  api.pledgers.listAll.mockResolvedValue([pledger('pledger-1', 'Ada Lovelace')])
  api.pledgeCategories.list.mockResolvedValue([category('category-1', 'General Fund')])
  api.pledges.listAllByPledger.mockResolvedValue([pledge('pledge-1')])
  return api
}

describe('PaymentsView', () => {
  it('lists payments for the selected pledge', async () => {
    const api = setup()
    api.pledgePayments.listByPledge.mockResolvedValue(page([payment()]))

    renderWithApi(
      <PaymentsView selection={{ pledgerId: 'pledger-1', pledgeId: 'pledge-1' }} onSelectionChange={vi.fn()} />,
      api,
    )

    expect(await screen.findByText('Monthly payment')).toBeInTheDocument()
    expect(api.pledgePayments.listByPledge).toHaveBeenCalledWith(
      'pledge-1',
      { page: 1, pageSize: 20, sortBy: 'PaymentDate', sortOrder: 'Descending' },
      expect.any(AbortSignal),
    )
  })

  it('records a payment using the entered calendar date', async () => {
    const api = setup()
    api.pledgePayments.listByPledge.mockResolvedValue(page([]))
    api.pledgePayments.create.mockResolvedValue(payment())
    const user = userEvent.setup()

    renderWithApi(
      <PaymentsView selection={{ pledgerId: 'pledger-1', pledgeId: 'pledge-1' }} onSelectionChange={vi.fn()} />,
      api,
    )
    await user.click(await screen.findByRole('button', { name: 'Record payment' }))
    const form = within(screen.getByRole('dialog', { name: 'Record payment' }))
    await user.type(form.getByLabelText('Amount'), '50')
    await user.type(form.getByLabelText('Payment date'), '2026-01-20')
    await user.click(form.getByRole('button', { name: 'Record payment' }))

    expect(api.pledgePayments.create).toHaveBeenCalledWith({
      pledgeId: 'pledge-1',
      amount: 50,
      paymentDate: '2026-01-20T00:00:00',
      comment: null,
    })
  })

  it('updates a payment with the active flag and preserves an unchanged date value', async () => {
    const api = setup()
    api.pledgePayments.listByPledge.mockResolvedValue(page([payment({ paymentDate: '2026-01-20T09:15:00' })]))
    api.pledgePayments.update.mockResolvedValue(payment())
    const user = userEvent.setup()

    renderWithApi(
      <PaymentsView selection={{ pledgerId: 'pledger-1', pledgeId: 'pledge-1' }} onSelectionChange={vi.fn()} />,
      api,
    )
    await user.click(await screen.findByRole('button', { name: /^Edit the payment/ }))
    const form = within(screen.getByRole('dialog', { name: 'Edit payment' }))
    await user.click(form.getByLabelText('Active'))
    await user.click(form.getByRole('button', { name: 'Save changes' }))

    expect(api.pledgePayments.update).toHaveBeenCalledWith('payment-1', {
      pledgeId: 'pledge-1',
      amount: 50,
      paymentDate: '2026-01-20T09:15:00',
      comment: 'Monthly payment',
      active: false,
    })
  })

  it('guides the user to select a pledger first', async () => {
    const api = setup()

    renderWithApi(<PaymentsView selection={{ pledgerId: '', pledgeId: '' }} onSelectionChange={vi.fn()} />, api)

    expect(await screen.findByText(/Select a pledger, then one of their pledges/)).toBeInTheDocument()
    expect(api.pledgePayments.listByPledge).not.toHaveBeenCalled()
  })
})

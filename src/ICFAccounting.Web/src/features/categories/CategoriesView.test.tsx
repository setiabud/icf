import { screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it } from 'vitest'
import { category, createFakeApi, renderWithApi } from '../../test/testUtils'
import { CategoriesView } from './CategoriesView'

describe('CategoriesView', () => {
  it('shows inactive categories and validates a new category before saving', async () => {
    const api = createFakeApi()
    api.pledgeCategories.list.mockResolvedValue([category('cat-1', 'General Fund'), category('cat-2', 'Legacy', false)])
    api.pledgeCategories.create.mockResolvedValue(category('cat-3', 'Missions'))
    const user = userEvent.setup()

    renderWithApi(<CategoriesView onViewPledges={() => {}} />, api)

    expect(await screen.findByText('General Fund')).toBeInTheDocument()
    expect(screen.getByText('Legacy')).toBeInTheDocument()
    expect(screen.getByText('Inactive')).toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: 'New category' }))
    const form = within(screen.getByRole('dialog', { name: 'New category' }))
    await user.click(form.getByRole('button', { name: 'Create category' }))
    expect(form.getByText('Category name is required.')).toBeInTheDocument()
    expect(api.pledgeCategories.create).not.toHaveBeenCalled()

    await user.type(form.getByLabelText('Category name'), ' Missions ')
    await user.click(form.getByRole('button', { name: 'Create category' }))
    expect(api.pledgeCategories.create).toHaveBeenCalledWith({ categoryName: 'Missions' })
  })
})

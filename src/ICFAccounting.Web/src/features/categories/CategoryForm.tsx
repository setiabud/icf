import { useState, type FormEvent } from 'react'
import type { PledgeCategory } from '../../api/types'
import { ErrorAlert } from '../../components/Alert'
import { Field } from '../../components/Field'
import { fieldDescribedBy } from '../../lib/fieldDescribedBy'
import { useSubmission } from '../../hooks/useSubmission'
import { validateRequiredName } from '../../lib/validation'

export interface CategoryFormValues {
  categoryName: string
  active: boolean
}

interface CategoryFormProps {
  category?: PledgeCategory
  onSubmit: (values: CategoryFormValues) => Promise<PledgeCategory>
  onSaved: (category: PledgeCategory) => void
  onCancel: () => void
}

export function CategoryForm({ category, onSubmit, onSaved, onCancel }: CategoryFormProps) {
  const [categoryName, setCategoryName] = useState(category?.categoryName ?? '')
  const [active, setActive] = useState(category?.active ?? true)
  const [nameError, setNameError] = useState<string>()
  const { pending, error, submit } = useSubmission()

  async function handleSubmit(event: FormEvent) {
    event.preventDefault()
    const validationError = validateRequiredName(categoryName, 'Category name')
    setNameError(validationError)
    if (validationError) {
      return
    }
    const result = await submit(() => onSubmit({ categoryName: categoryName.trim(), active }))
    if (result.ok) {
      onSaved(result.value)
    }
  }

  return (
    <form onSubmit={handleSubmit} noValidate>
      <Field id="category-name" label="Category name" error={nameError}>
        <input
          id="category-name"
          value={categoryName}
          onChange={(event) => setCategoryName(event.target.value)}
          aria-invalid={nameError ? true : undefined}
          aria-describedby={fieldDescribedBy('category-name', nameError)}
          autoComplete="off"
          required
        />
      </Field>
      {category && (
        <label className="checkbox">
          <input type="checkbox" checked={active} onChange={(event) => setActive(event.target.checked)} />
          Active
        </label>
      )}
      {error !== undefined && <ErrorAlert error={error} action="save the category" />}
      <div className="form-actions">
        <button type="button" className="button button--secondary" onClick={onCancel} disabled={pending}>
          Cancel
        </button>
        <button type="submit" className="button button--primary" disabled={pending}>
          {pending ? 'Saving…' : category ? 'Save changes' : 'Create category'}
        </button>
      </div>
    </form>
  )
}

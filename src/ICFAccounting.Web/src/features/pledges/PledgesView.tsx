import { ErrorAlert } from '../../components/Alert'
import { EmptyState, LoadingState } from '../../components/States'
import { optionLabel, useReferenceData } from '../../hooks/useReferenceData'
import type { Pledge } from '../../api/types'
import { PledgeList } from './PledgeList'
import type { PledgeParent, PledgeParentKind } from './PledgeParent'

interface PledgesViewProps {
  parent: PledgeParent
  onParentChange: (parent: PledgeParent) => void
  onViewPayments: (pledge: Pledge) => void
}

export function PledgesView({ parent, onParentChange, onViewPayments }: PledgesViewProps) {
  const { state, reload } = useReferenceData()
  const referenceData = state.data

  const parentOptions =
    parent.kind === 'pledger'
      ? (referenceData?.pledgers ?? []).map((pledger) => ({
          id: pledger.id,
          label: optionLabel(pledger.pledgerName, pledger.active),
        }))
      : (referenceData?.categories ?? []).map((category) => ({
          id: category.id,
          label: optionLabel(category.categoryName, category.active),
        }))
  const parentExists = parentOptions.some((option) => option.id === parent.id)

  return (
    <section aria-labelledby="pledges-heading">
      <div className="section-header">
        <h2 id="pledges-heading">Pledges</h2>
      </div>

      <fieldset className="selector">
        <legend>Show pledges for</legend>
        <div className="segmented">
          {(['pledger', 'category'] as PledgeParentKind[]).map((kind) => (
            <label key={kind} className={`segmented__option${parent.kind === kind ? ' is-selected' : ''}`}>
              <input
                type="radio"
                name="pledge-parent-kind"
                value={kind}
                checked={parent.kind === kind}
                onChange={() => onParentChange({ kind, id: '' })}
              />
              {kind === 'pledger' ? 'Pledger' : 'Category'}
            </label>
          ))}
        </div>
        <label className="inline-field">
          <span className="visually-hidden">{parent.kind === 'pledger' ? 'Pledger' : 'Pledge category'}</span>
          <select
            value={parentExists ? parent.id : ''}
            onChange={(event) => onParentChange({ kind: parent.kind, id: event.target.value })}
            disabled={!referenceData}
          >
            <option value="">{parent.kind === 'pledger' ? 'Select a pledger' : 'Select a category'}</option>
            {parentOptions.map((option) => (
              <option key={option.id} value={option.id}>
                {option.label}
              </option>
            ))}
          </select>
        </label>
      </fieldset>

      {state.status === 'error' && (
        <ErrorAlert error={state.error} action="load pledgers and categories" onRetry={reload} />
      )}
      {state.status === 'loading' && !referenceData && <LoadingState label="Loading pledgers and categories…" />}
      {referenceData && !parentExists && (
        <EmptyState>
          <p>
            {parentOptions.length === 0
              ? `There are no ${parent.kind === 'pledger' ? 'pledgers' : 'pledge categories'} yet. Create one first.`
              : `Select a ${parent.kind === 'pledger' ? 'pledger' : 'category'} to see its pledges.`}
          </p>
        </EmptyState>
      )}
      {referenceData && parentExists && parent.id && (
        <PledgeList
          key={`${parent.kind}:${parent.id}`}
          parent={{ kind: parent.kind, id: parent.id }}
          referenceData={referenceData}
          onViewPayments={onViewPayments}
        />
      )}
    </section>
  )
}

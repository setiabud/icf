import { useApi } from '../../api/ApiContext'
import type { Guid, Pledge } from '../../api/types'
import { ErrorAlert } from '../../components/Alert'
import { EmptyState, LoadingState } from '../../components/States'
import { useAsyncData } from '../../hooks/useAsyncData'
import { optionLabel, useReferenceData, type ReferenceData } from '../../hooks/useReferenceData'
import { formatAmount } from '../../lib/format'
import { PaymentList } from './PaymentList'

export interface PaymentSelection {
  pledgerId: Guid | ''
  pledgeId: Guid | ''
}

interface PaymentsViewProps {
  selection: PaymentSelection
  onSelectionChange: (selection: PaymentSelection) => void
}

function describePledge(pledge: Pledge, referenceData: ReferenceData): string {
  return `${pledge.year} · ${referenceData.categoryName(pledge.pledgeCategoryId)} · ${formatAmount(pledge.amount)}`
}

export function PaymentsView({ selection, onSelectionChange }: PaymentsViewProps) {
  const api = useApi()
  const { state: referenceState, reload: reloadReference } = useReferenceData()
  const referenceData = referenceState.data
  const { pledgerId, pledgeId } = selection

  const { state: pledgesState, reload: reloadPledges } = useAsyncData(
    (signal) => (pledgerId ? api.pledges.listAllByPledger(pledgerId, signal) : Promise.resolve([])),
    [api, pledgerId],
  )
  const pledgesLoaded = pledgesState.status === 'success'
  const pledges = pledgesLoaded ? pledgesState.data : []
  const selectedPledge = pledges.find((pledge) => pledge.id === pledgeId)

  return (
    <section aria-labelledby="payments-heading">
      <div className="section-header">
        <h2 id="payments-heading">Payments</h2>
      </div>

      <fieldset className="selector">
        <legend>Select a pledge</legend>
        <label className="inline-field">
          Pledger
          <select
            value={pledgerId}
            onChange={(event) => onSelectionChange({ pledgerId: event.target.value, pledgeId: '' })}
            disabled={!referenceData}
          >
            <option value="">Select a pledger</option>
            {referenceData?.pledgers.map((pledger) => (
              <option key={pledger.id} value={pledger.id}>
                {optionLabel(pledger.pledgerName, pledger.active)}
              </option>
            ))}
          </select>
        </label>
        <label className="inline-field">
          Pledge
          <select
            value={selectedPledge ? pledgeId : ''}
            onChange={(event) => onSelectionChange({ pledgerId, pledgeId: event.target.value })}
            disabled={!referenceData || !pledgerId || !pledgesLoaded || pledges.length === 0}
          >
            <option value="">
              {pledgerId && pledgesLoaded && pledges.length === 0 ? 'No pledges' : 'Select a pledge'}
            </option>
            {referenceData &&
              pledges.map((pledge) => (
                <option key={pledge.id} value={pledge.id}>
                  {describePledge(pledge, referenceData)}
                </option>
              ))}
          </select>
        </label>
      </fieldset>

      {referenceState.status === 'error' && (
        <ErrorAlert error={referenceState.error} action="load pledgers and categories" onRetry={reloadReference} />
      )}
      {pledgesState.status === 'error' && (
        <ErrorAlert error={pledgesState.error} action="load the pledger's pledges" onRetry={reloadPledges} />
      )}
      {(referenceState.status === 'loading' || (pledgerId && pledgesState.status === 'loading')) && (
        <LoadingState label="Loading pledges…" />
      )}

      {referenceData && pledgesLoaded && !selectedPledge && (
        <EmptyState>
          <p>
            {!pledgerId
              ? 'Select a pledger, then one of their pledges, to see its payments.'
              : pledges.length === 0
                ? `${referenceData.pledgerName(pledgerId)} has no pledges yet. Record a pledge before adding payments.`
                : pledgeId
                  ? 'The selected pledge no longer exists. Choose another pledge.'
                  : 'Select a pledge to see its payments.'}
          </p>
        </EmptyState>
      )}

      {referenceData && selectedPledge && (
        <PaymentList
          key={selectedPledge.id}
          pledge={selectedPledge}
          pledgeDescription={`${referenceData.pledgerName(selectedPledge.pledgerId)} — ${describePledge(selectedPledge, referenceData)}`}
        />
      )}
    </section>
  )
}

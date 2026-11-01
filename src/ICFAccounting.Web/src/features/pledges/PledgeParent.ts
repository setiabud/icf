import type { Guid } from '../../api/types'

export type PledgeParentKind = 'pledger' | 'category'

export interface PledgeParent {
  kind: PledgeParentKind
  id: Guid | ''
}

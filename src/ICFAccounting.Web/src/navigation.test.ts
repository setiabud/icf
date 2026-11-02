import { describe, expect, it } from 'vitest'
import { readRoute, routeHash } from './navigation'
import { buildUrl } from './api/httpClient'

describe('protected navigation', () => {
  it.each(['#/pledgers', '#/categories', '#/pledges?kind=category&parentId=cat-1',
    '#/payments?pledgerId=person-1&pledgeId=pledge-1'])('round-trips intended page %s', (hash) => {
    expect(routeHash(readRoute(hash))).toBe(hash)
  })

  it.each(['#//evil.example', '#https://evil.example', '#/login?returnTo=https://evil.example'])(
    'does not treat unrecognized destinations as redirects: %s', (hash) => {
      expect(readRoute(hash).section).toBe('pledgers')
      expect(routeHash(readRoute(hash))).toBe('#/pledgers')
    },
  )

  it('resolves the default same-origin API prefix', () => {
    expect(buildUrl('/api', '/auth/session')).toBe(`${window.location.origin}/api/auth/session`)
  })
})

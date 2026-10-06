import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { enumAccountDeviceLinks, enumAllAccounts, setAccountEnabled, setAccountRole } from '../api/admin'
import { useApiCall } from '../api/useApiCall'
import { AccountRole, ResponseState, type AdminAccountSummary } from '../api/types'
import { parseWcfDate } from '../api/wcfDate'
import './DataPage.css'

const ROLE_LABEL: Record<AccountRole, string> = {
  [AccountRole.EndUser]: 'End user',
  [AccountRole.Installer]: 'Installer',
  [AccountRole.Admin]: 'Admin',
}

const ROLE_OPTIONS = [AccountRole.EndUser, AccountRole.Installer, AccountRole.Admin]

type RoleFilter = 'all' | AccountRole
type StatusFilter = 'all' | 'enabled' | 'disabled'
type SortKey = 'name' | 'role' | 'devices' | 'created' | 'lastLogin'
type SortDir = 'asc' | 'desc'
const PAGE_SIZE = 25

export function AccountsPage() {
  const call = useApiCall()
  const [accounts, setAccounts] = useState<AdminAccountSummary[] | null>(null)
  const [deviceCounts, setDeviceCounts] = useState<Map<number, number> | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [search, setSearch] = useState('')
  const [roleFilter, setRoleFilter] = useState<RoleFilter>('all')
  const [statusFilter, setStatusFilter] = useState<StatusFilter>('all')
  const [sortKey, setSortKey] = useState<SortKey>('created')
  const [sortDir, setSortDir] = useState<SortDir>('desc')
  const [page, setPage] = useState(1)
  const [savingId, setSavingId] = useState<number | null>(null)
  const [actionError, setActionError] = useState<string | null>(null)

  useEffect(() => {
    Promise.all([call(() => enumAllAccounts()), call(() => enumAccountDeviceLinks())])
      .then(([accountsRes, linksRes]) => {
        if (!accountsRes.Accounts) {
          setError(accountsRes.Message || 'Could not load accounts')
          return
        }
        setAccounts(accountsRes.Accounts)
        const counts = new Map<number, number>()
        for (const link of linksRes.Links ?? []) {
          counts.set(link.AccountId, (counts.get(link.AccountId) ?? 0) + 1)
        }
        setDeviceCounts(counts)
      })
      .catch(() => setError('Could not reach the server.'))
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  async function handleRoleChange(account: AdminAccountSummary, role: AccountRole) {
    if (role === account.Role) return
    if (!window.confirm(`Change ${account.Email}'s role from ${ROLE_LABEL[account.Role]} to ${ROLE_LABEL[role]}?`)) {
      return
    }
    setActionError(null)
    setSavingId(account.AccountId)
    try {
      const res = await call(() => setAccountRole(account.AccountId, role))
      if (res.State !== ResponseState.OK) {
        setActionError(res.Message || 'Could not update role.')
        return
      }
      setAccounts((prev) => prev && prev.map((a) => (a.AccountId === account.AccountId ? { ...a, Role: role } : a)))
    } catch {
      setActionError('Could not reach the server.')
    } finally {
      setSavingId(null)
    }
  }

  async function handleToggleEnabled(account: AdminAccountSummary) {
    const next = !account.Enabled
    const confirmText = next
      ? `Enable ${account.Email}? They'll be able to log in again.`
      : `Disable ${account.Email}? This force-logs them out of every active session and blocks further logins.`
    if (!window.confirm(confirmText)) return
    setActionError(null)
    setSavingId(account.AccountId)
    try {
      const res = await call(() => setAccountEnabled(account.AccountId, next))
      if (res.State !== ResponseState.OK) {
        setActionError(res.Message || 'Could not update account status.')
        return
      }
      setAccounts((prev) => prev && prev.map((a) => (a.AccountId === account.AccountId ? { ...a, Enabled: next } : a)))
    } catch {
      setActionError('Could not reach the server.')
    } finally {
      setSavingId(null)
    }
  }

  function toggleSort(key: SortKey) {
    if (key === sortKey) {
      setSortDir((d) => (d === 'asc' ? 'desc' : 'asc'))
    } else {
      setSortKey(key)
      setSortDir('asc')
    }
    setPage(1)
  }

  const filtered = accounts?.filter((a) => {
    const q = search.trim().toLowerCase()
    if (q && !a.FirstName.toLowerCase().includes(q) && !a.LastName.toLowerCase().includes(q) && !a.Email.toLowerCase().includes(q)) {
      return false
    }
    if (roleFilter !== 'all' && a.Role !== roleFilter) return false
    if (statusFilter === 'enabled' && !a.Enabled) return false
    if (statusFilter === 'disabled' && a.Enabled) return false
    return true
  })

  const sorted = filtered
    ? [...filtered].sort((a, b) => {
        let cmp = 0
        switch (sortKey) {
          case 'name':
            cmp = `${a.FirstName} ${a.LastName}`.localeCompare(`${b.FirstName} ${b.LastName}`)
            break
          case 'role':
            cmp = a.Role - b.Role
            break
          case 'devices':
            cmp = (deviceCounts?.get(a.AccountId) ?? 0) - (deviceCounts?.get(b.AccountId) ?? 0)
            break
          case 'lastLogin':
            cmp = (parseWcfDate(a.LastLoginDateTime)?.getTime() ?? 0) - (parseWcfDate(b.LastLoginDateTime)?.getTime() ?? 0)
            break
          case 'created':
          default:
            cmp = (parseWcfDate(a.CreatedDateTime)?.getTime() ?? 0) - (parseWcfDate(b.CreatedDateTime)?.getTime() ?? 0)
        }
        return sortDir === 'asc' ? cmp : -cmp
      })
    : null

  const totalPages = sorted ? Math.max(1, Math.ceil(sorted.length / PAGE_SIZE)) : 1
  const clampedPage = Math.min(page, totalPages)
  const paged = sorted?.slice((clampedPage - 1) * PAGE_SIZE, clampedPage * PAGE_SIZE)

  function resetToFirstPage() {
    setPage(1)
  }

  function sortIndicator(key: SortKey) {
    if (key !== sortKey) return ''
    return sortDir === 'asc' ? ' ↑' : ' ↓'
  }

  return (
    <div>
      <h1>Accounts</h1>
      <p className="page-sub">Every account across the fleet, not just one user's own.</p>

      {error && <div className="page-error">{error}</div>}
      {actionError && <div className="page-error">{actionError}</div>}

      {!error && !accounts && <div className="page-loading">Loading...</div>}

      {accounts && (
        <>
          <div className="filter-row">
            <input
              className="search-input"
              type="search"
              placeholder="Search by name or email..."
              value={search}
              onChange={(e) => {
                setSearch(e.target.value)
                resetToFirstPage()
              }}
            />
            <select
              className="role-select"
              value={roleFilter}
              onChange={(e) => {
                setRoleFilter(e.target.value === 'all' ? 'all' : (Number(e.target.value) as AccountRole))
                resetToFirstPage()
              }}
            >
              <option value="all">All roles</option>
              {ROLE_OPTIONS.map((r) => (
                <option key={r} value={r}>{ROLE_LABEL[r]}</option>
              ))}
            </select>
            <select
              className="role-select"
              value={statusFilter}
              onChange={(e) => {
                setStatusFilter(e.target.value as StatusFilter)
                resetToFirstPage()
              }}
            >
              <option value="all">Enabled + disabled</option>
              <option value="enabled">Enabled only</option>
              <option value="disabled">Disabled only</option>
            </select>
          </div>
          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th><button type="button" className="sort-btn" onClick={() => toggleSort('name')}>Name{sortIndicator('name')}</button></th>
                  <th>Email</th>
                  <th><button type="button" className="sort-btn" onClick={() => toggleSort('role')}>Role{sortIndicator('role')}</button></th>
                  <th><button type="button" className="sort-btn" onClick={() => toggleSort('devices')}>Panels{sortIndicator('devices')}</button></th>
                  <th>Status</th>
                  <th><button type="button" className="sort-btn" onClick={() => toggleSort('created')}>Created{sortIndicator('created')}</button></th>
                  <th><button type="button" className="sort-btn" onClick={() => toggleSort('lastLogin')}>Last login{sortIndicator('lastLogin')}</button></th>
                  <th></th>
                </tr>
              </thead>
              <tbody>
                {paged!.map((a) => (
                  <tr key={a.AccountId}>
                    <td>
                      <Link className="row-link" to={`/accounts/${a.AccountId}`}>
                        {a.FirstName} {a.LastName}
                      </Link>
                    </td>
                    <td>{a.Email}</td>
                    <td>
                      <span className={`pill role-${a.Role}`}>{ROLE_LABEL[a.Role]}</span>
                      <select
                        className="role-select"
                        value={a.Role}
                        disabled={savingId === a.AccountId}
                        onChange={(e) => handleRoleChange(a, Number(e.target.value) as AccountRole)}
                      >
                        {ROLE_OPTIONS.map((r) => (
                          <option key={r} value={r}>{ROLE_LABEL[r]}</option>
                        ))}
                      </select>
                    </td>
                    <td>{deviceCounts?.get(a.AccountId) ?? 0}</td>
                    <td>
                      <span className={a.Enabled ? 'pill ok' : 'pill off'}>{a.Enabled ? 'Enabled' : 'Disabled'}</span>
                    </td>
                    <td>{parseWcfDate(a.CreatedDateTime)?.toLocaleDateString() ?? '—'}</td>
                    <td>{parseWcfDate(a.LastLoginDateTime)?.toLocaleString() ?? 'Never'}</td>
                    <td>
                      <button
                        type="button"
                        className={a.Enabled ? 'action-btn danger' : 'action-btn'}
                        disabled={savingId === a.AccountId}
                        onClick={() => handleToggleEnabled(a)}
                      >
                        {a.Enabled ? 'Disable' : 'Enable'}
                      </button>
                    </td>
                  </tr>
                ))}
                {sorted!.length === 0 && (
                  <tr>
                    <td colSpan={8} className="empty-row">
                      {accounts.length === 0 ? 'No accounts yet.' : 'No accounts match your filters.'}
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
          {sorted!.length > 0 && (
            <div className="pagination">
              <button type="button" className="action-btn" disabled={clampedPage <= 1} onClick={() => setPage(clampedPage - 1)}>
                &larr; Prev
              </button>
              <span className="pagination-status">
                Page {clampedPage} of {totalPages} &middot; {sorted!.length} account{sorted!.length === 1 ? '' : 's'}
              </span>
              <button type="button" className="action-btn" disabled={clampedPage >= totalPages} onClick={() => setPage(clampedPage + 1)}>
                Next &rarr;
              </button>
            </div>
          )}
        </>
      )}
    </div>
  )
}

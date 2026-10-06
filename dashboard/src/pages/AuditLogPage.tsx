import { useEffect, useMemo, useState } from 'react'
import { enumAuditLog } from '../api/admin'
import { useApiCall } from '../api/useApiCall'
import type { AuditLogEntry } from '../api/types'
import { parseWcfDate } from '../api/wcfDate'
import './DataPage.css'

const ACTION_LABEL: Record<string, string> = {
  SetAccountRole: 'Changed role',
  SetAccountEnabled: 'Changed account status',
  EnableAccount: 'Enabled account',
  DisableAccount: 'Disabled account',
  RevokeAccountSessions: 'Revoked sessions',
  UnlinkAccountDevice: 'Unlinked device',
  BlockDevice: 'Blocked PIN',
  ResetDevice: 'Reset PIN',
  DisassociateDevice: 'Disassociated',
}

const PAGE_SIZE = 25

export function AuditLogPage() {
  const call = useApiCall()
  const [entries, setEntries] = useState<AuditLogEntry[] | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [search, setSearch] = useState('')
  const [actorFilter, setActorFilter] = useState('all')
  const [actionFilter, setActionFilter] = useState('all')
  const [targetTypeFilter, setTargetTypeFilter] = useState('all')
  const [fromDate, setFromDate] = useState('')
  const [toDate, setToDate] = useState('')
  const [page, setPage] = useState(1)

  useEffect(() => {
    call(() => enumAuditLog())
      .then((res) => {
        if (res.Entries) {
          setEntries(res.Entries)
        } else {
          setError(res.Message || 'Could not load the audit log')
        }
      })
      .catch(() => setError('Could not reach the server.'))
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  const actors = useMemo(() => {
    const set = new Set<string>()
    entries?.forEach((e) => set.add(e.ActorEmail ?? `Account #${e.ActorAccountId}`))
    return [...set].sort()
  }, [entries])

  const actions = useMemo(() => {
    const set = new Set<string>()
    entries?.forEach((e) => set.add(e.Action))
    return [...set].sort()
  }, [entries])

  const targetTypes = useMemo(() => {
    const set = new Set<string>()
    entries?.forEach((e) => set.add(e.TargetType))
    return [...set].sort()
  }, [entries])

  function resetToFirstPage() {
    setPage(1)
  }

  const filtered = entries?.filter((e) => {
    const q = search.trim().toLowerCase()
    if (q) {
      const haystack = `${e.ActorEmail ?? ''} ${ACTION_LABEL[e.Action] ?? e.Action} ${e.TargetType} ${e.TargetId ?? ''} ${e.Details ?? ''}`.toLowerCase()
      if (!haystack.includes(q)) return false
    }
    const actor = e.ActorEmail ?? `Account #${e.ActorAccountId}`
    if (actorFilter !== 'all' && actor !== actorFilter) return false
    if (actionFilter !== 'all' && e.Action !== actionFilter) return false
    if (targetTypeFilter !== 'all' && e.TargetType !== targetTypeFilter) return false
    const when = parseWcfDate(e.CreatedDateTime)
    if (fromDate && when && when < new Date(fromDate)) return false
    if (toDate && when) {
      const end = new Date(toDate)
      end.setHours(23, 59, 59, 999)
      if (when > end) return false
    }
    return true
  })

  const totalPages = filtered ? Math.max(1, Math.ceil(filtered.length / PAGE_SIZE)) : 1
  const clampedPage = Math.min(page, totalPages)
  const paged = filtered?.slice((clampedPage - 1) * PAGE_SIZE, clampedPage * PAGE_SIZE)

  return (
    <div>
      <h1>Audit log</h1>
      <p className="page-sub">The last 200 admin actions -- who did what, and when.</p>

      {error && <div className="page-error">{error}</div>}

      {!error && !entries && <div className="page-loading">Loading...</div>}

      {entries && (
        <>
          <div className="filter-row">
            <input
              className="search-input"
              type="search"
              placeholder="Search..."
              value={search}
              onChange={(e) => {
                setSearch(e.target.value)
                resetToFirstPage()
              }}
            />
            <select className="role-select" value={actorFilter} onChange={(e) => { setActorFilter(e.target.value); resetToFirstPage() }}>
              <option value="all">All admins</option>
              {actors.map((a) => (
                <option key={a} value={a}>{a}</option>
              ))}
            </select>
            <select className="role-select" value={actionFilter} onChange={(e) => { setActionFilter(e.target.value); resetToFirstPage() }}>
              <option value="all">All actions</option>
              {actions.map((a) => (
                <option key={a} value={a}>{ACTION_LABEL[a] ?? a}</option>
              ))}
            </select>
            <select className="role-select" value={targetTypeFilter} onChange={(e) => { setTargetTypeFilter(e.target.value); resetToFirstPage() }}>
              <option value="all">All target types</option>
              {targetTypes.map((t) => (
                <option key={t} value={t}>{t}</option>
              ))}
            </select>
            <input
              className="inline-input"
              type="date"
              value={fromDate}
              onChange={(e) => { setFromDate(e.target.value); resetToFirstPage() }}
              aria-label="From date"
            />
            <span className="page-sub" style={{ margin: 0 }}>to</span>
            <input
              className="inline-input"
              type="date"
              value={toDate}
              onChange={(e) => { setToDate(e.target.value); resetToFirstPage() }}
              aria-label="To date"
            />
          </div>
          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>When</th>
                  <th>Admin</th>
                  <th>Action</th>
                  <th>Target</th>
                  <th>Details</th>
                  <th>Result</th>
                </tr>
              </thead>
              <tbody>
                {paged!.map((e) => (
                  <tr key={e.AuditLogId}>
                    <td>{parseWcfDate(e.CreatedDateTime)?.toLocaleString() ?? '—'}</td>
                    <td>{e.ActorEmail ?? `Account #${e.ActorAccountId}`}</td>
                    <td>{ACTION_LABEL[e.Action] ?? e.Action}</td>
                    <td>{e.TargetType}{e.TargetId != null ? ` #${e.TargetId}` : ''}</td>
                    <td>{e.Details ?? '—'}</td>
                    <td><span className="pill ok">Success</span></td>
                  </tr>
                ))}
                {filtered!.length === 0 && (
                  <tr>
                    <td colSpan={6} className="empty-row">
                      {entries.length === 0 ? 'No admin actions logged yet.' : 'No entries match your filters.'}
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
          {filtered!.length > 0 && (
            <div className="pagination">
              <button type="button" className="action-btn" disabled={clampedPage <= 1} onClick={() => setPage(clampedPage - 1)}>
                &larr; Prev
              </button>
              <span className="pagination-status">
                Page {clampedPage} of {totalPages} &middot; {filtered!.length} entr{filtered!.length === 1 ? 'y' : 'ies'}
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

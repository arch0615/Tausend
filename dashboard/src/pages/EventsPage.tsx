import { Fragment, useEffect, useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import { enumAccountDeviceLinks, enumAllAccounts, enumAllDevices, enumAllEvents } from '../api/admin'
import { useApiCall } from '../api/useApiCall'
import type { AdminAccountSummary, AdminDeviceSummary, AlarmEvent } from '../api/types'
import { parseWcfDate } from '../api/wcfDate'
import './DataPage.css'

// Mirrors backend-core's Enums/NotificationTypes.cs exactly -- the only source-verified mapping
// of these numeric codes to what they mean. Anything not in this map is shown as "Type <n>"
// rather than guessed at.
const NOTIFICATION_TYPE_LABEL: Record<number, string> = {
  1: 'Test',
  100: 'Medical',
  101: 'Personal medical',
  110: 'Fire',
  120: 'Panic',
  121: 'Assault',
  122: 'Silent panic',
  123: 'Keypad assault',
  130: 'Stole',
  137: 'Sabotage',
  139: 'Zone cross',
  151: 'Gas',
  146: 'Silent alarm',
  300: 'Invalid/fail',
  301: 'Main power fail',
  302: 'Battery fail',
  306: 'Programming',
  321: 'Siren 1 fail',
  322: 'Siren 2 fail',
  330: 'Bus fail',
  337: 'Auxiliary 12V fail',
  351: 'Phone line fail',
  354: 'Communicator fail',
  355: 'Server communication fail',
  401: 'Armed / disarmed',
  403: 'Auto arm',
  405: 'Auto arm canceled',
  406: 'Bell disarm',
  408: 'Fast arm',
  409: 'Key arm/disarm',
  422: 'User access control',
  426: 'Point access control',
  455: 'Auto arm fail',
  456: 'Partial arm',
  458: 'Memory disarmed',
  459: 'Recent armed alarm',
  570: 'Zone bypass',
  601: 'Manual test',
  602: 'Periodic test',
  625: 'Clock fail',
  630: 'User link fail',
  631: 'User linked',
  651: 'Unknown event',
}

const EVENT_TYPE_LABEL: Record<string, string> = { E: 'Event', R: 'Restore' }

function typeLabel(e: AlarmEvent): string {
  return NOTIFICATION_TYPE_LABEL[e.NotificationType] ?? `Type ${e.NotificationType}`
}

const PAGE_SIZE = 25

export function EventsPage() {
  const call = useApiCall()
  const [events, setEvents] = useState<AlarmEvent[] | null>(null)
  const [devices, setDevices] = useState<AdminDeviceSummary[] | null>(null)
  const [accounts, setAccounts] = useState<AdminAccountSummary[] | null>(null)
  const [linkedAccountIds, setLinkedAccountIds] = useState<Map<number, number[]>>(new Map())
  const [error, setError] = useState<string | null>(null)
  const [search, setSearch] = useState('')
  const [deviceFilter, setDeviceFilter] = useState('all')
  const [accountFilter, setAccountFilter] = useState('all')
  const [typeFilter, setTypeFilter] = useState('all')
  const [fromDate, setFromDate] = useState('')
  const [toDate, setToDate] = useState('')
  const [page, setPage] = useState(1)
  const [expanded, setExpanded] = useState<number | null>(null)

  useEffect(() => {
    Promise.all([call(() => enumAllEvents()), call(() => enumAllDevices()), call(() => enumAllAccounts()), call(() => enumAccountDeviceLinks())])
      .then(([eventsRes, devicesRes, accountsRes, linksRes]) => {
        if (!eventsRes.Events) {
          setError(eventsRes.Message || 'Could not load events')
          return
        }
        setEvents(eventsRes.Events)
        setDevices(devicesRes.Devices ?? [])
        setAccounts(accountsRes.Accounts ?? [])
        const byDevice = new Map<number, number[]>()
        for (const link of linksRes.Links ?? []) {
          byDevice.set(link.DeviceId, [...(byDevice.get(link.DeviceId) ?? []), link.AccountId])
        }
        setLinkedAccountIds(byDevice)
      })
      .catch(() => setError('Could not reach the server.'))
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  const deviceByIdentifier = useMemo(() => {
    const map = new Map<string, AdminDeviceSummary>()
    devices?.forEach((d) => map.set(d.Identifier, d))
    return map
  }, [devices])

  const accountById = useMemo(() => {
    const map = new Map<number, AdminAccountSummary>()
    accounts?.forEach((a) => map.set(a.AccountId, a))
    return map
  }, [accounts])

  function accountsForEvent(e: AlarmEvent): AdminAccountSummary[] {
    const device = e.AlarmIdentifier ? deviceByIdentifier.get(e.AlarmIdentifier) : undefined
    if (!device) return []
    const ids = linkedAccountIds.get(device.DeviceId) ?? []
    return ids.map((id) => accountById.get(id)).filter((a): a is AdminAccountSummary => a != null)
  }

  const presentTypes = useMemo(() => {
    const set = new Set<number>()
    events?.forEach((e) => set.add(e.NotificationType))
    return [...set].sort((a, b) => a - b)
  }, [events])

  function resetToFirstPage() {
    setPage(1)
  }

  const filtered = events?.filter((e) => {
    const device = e.AlarmIdentifier ? deviceByIdentifier.get(e.AlarmIdentifier) : undefined
    const evtAccounts = accountsForEvent(e)
    const q = search.trim().toLowerCase()
    if (q) {
      const haystack = `${device?.Description ?? ''} ${e.AlarmIdentifier ?? ''} ${typeLabel(e)} ${e.Text ?? ''} ${evtAccounts.map((a) => a.Email).join(' ')}`.toLowerCase()
      if (!haystack.includes(q)) return false
    }
    if (deviceFilter !== 'all' && String(device?.DeviceId) !== deviceFilter) return false
    if (accountFilter !== 'all' && !evtAccounts.some((a) => String(a.AccountId) === accountFilter)) return false
    if (typeFilter !== 'all' && String(e.NotificationType) !== typeFilter) return false
    const when = parseWcfDate(e.EventDateTime)
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
      <h1>Events</h1>
      <p className="page-sub">The last 200 panel events across the fleet -- who did what, on which panel, and when.</p>

      {error && <div className="page-error">{error}</div>}
      {!error && !events && <div className="page-loading">Loading...</div>}

      {events && (
        <>
          <div className="filter-row">
            <input
              className="search-input"
              type="search"
              placeholder="Search..."
              value={search}
              onChange={(e) => { setSearch(e.target.value); resetToFirstPage() }}
            />
            <select className="role-select" value={deviceFilter} onChange={(e) => { setDeviceFilter(e.target.value); resetToFirstPage() }}>
              <option value="all">All devices</option>
              {devices?.map((d) => (
                <option key={d.DeviceId} value={d.DeviceId}>{d.Description}</option>
              ))}
            </select>
            <select className="role-select" value={accountFilter} onChange={(e) => { setAccountFilter(e.target.value); resetToFirstPage() }}>
              <option value="all">All accounts</option>
              {accounts?.map((a) => (
                <option key={a.AccountId} value={a.AccountId}>{a.Email}</option>
              ))}
            </select>
            <select className="role-select" value={typeFilter} onChange={(e) => { setTypeFilter(e.target.value); resetToFirstPage() }}>
              <option value="all">All event types</option>
              {presentTypes.map((t) => (
                <option key={t} value={t}>{NOTIFICATION_TYPE_LABEL[t] ?? `Type ${t}`}</option>
              ))}
            </select>
            <input className="inline-input" type="date" value={fromDate} onChange={(e) => { setFromDate(e.target.value); resetToFirstPage() }} aria-label="From date" />
            <span className="page-sub" style={{ margin: 0 }}>to</span>
            <input className="inline-input" type="date" value={toDate} onChange={(e) => { setToDate(e.target.value); resetToFirstPage() }} aria-label="To date" />
          </div>
          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>When</th>
                  <th>Device</th>
                  <th>Account</th>
                  <th>Event</th>
                  <th>Details</th>
                </tr>
              </thead>
              <tbody>
                {paged!.map((e) => {
                  const device = e.AlarmIdentifier ? deviceByIdentifier.get(e.AlarmIdentifier) : undefined
                  const evtAccounts = accountsForEvent(e)
                  const isExpanded = expanded === e.EventId
                  return (
                    <Fragment key={e.EventId}>
                      <tr className="clickable-row" onClick={() => setExpanded(isExpanded ? null : e.EventId)}>
                        <td>{parseWcfDate(e.EventDateTime)?.toLocaleString() ?? e.StringDate ?? '—'}</td>
                        <td>
                          {device ? <Link className="row-link" to={`/devices/${device.DeviceId}`} onClick={(ev) => ev.stopPropagation()}>{device.Description}</Link> : (e.AlarmIdentifier ?? '—')}
                        </td>
                        <td>
                          {evtAccounts.length === 0
                            ? '—'
                            : evtAccounts.map((a, i) => (
                                <span key={a.AccountId}>
                                  {i > 0 && ', '}
                                  <Link className="row-link" to={`/accounts/${a.AccountId}`} onClick={(ev) => ev.stopPropagation()}>{a.Email}</Link>
                                </span>
                              ))}
                        </td>
                        <td>
                          <span className={`pill ${e.EventType === 'R' ? 'ok' : 'off'}`}>{EVENT_TYPE_LABEL[e.EventType ?? ''] ?? e.EventType ?? '—'}</span>
                          {' '}{typeLabel(e)}
                        </td>
                        <td>{e.Text || '—'}</td>
                      </tr>
                      {isExpanded && (
                        <tr className="detail-row">
                          <td colSpan={5}>
                            <div className="detail-grid">
                              <div><span className="detail-label">Event ID</span> {e.EventId}</div>
                              <div><span className="detail-label">Sequence</span> {e.Secuence}</div>
                              <div><span className="detail-label">Partition</span> {e.Partition}</div>
                              <div><span className="detail-label">Zone/user parameter</span> {e.AlarmParameter}</div>
                              <div><span className="detail-label">Notification type code</span> {e.NotificationType}</div>
                              <div><span className="detail-label">Panel identifier</span> {e.AlarmIdentifier ?? '—'}</div>
                            </div>
                          </td>
                        </tr>
                      )}
                    </Fragment>
                  )
                })}
                {filtered!.length === 0 && (
                  <tr>
                    <td colSpan={5} className="empty-row">
                      {events.length === 0 ? 'No events logged yet.' : 'No events match your filters.'}
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
          {filtered!.length > 0 && (
            <div className="pagination">
              <button type="button" className="action-btn" disabled={clampedPage <= 1} onClick={() => setPage(clampedPage - 1)}>&larr; Prev</button>
              <span className="pagination-status">
                Page {clampedPage} of {totalPages} &middot; {filtered!.length} event{filtered!.length === 1 ? '' : 's'}
              </span>
              <button type="button" className="action-btn" disabled={clampedPage >= totalPages} onClick={() => setPage(clampedPage + 1)}>Next &rarr;</button>
            </div>
          )}
        </>
      )}
    </div>
  )
}

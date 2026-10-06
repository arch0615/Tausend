import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { blockDeviceAsAdmin, disassociateDeviceAsAdmin, enumAllDevices, resetDeviceAsAdmin } from '../api/admin'
import { useApiCall } from '../api/useApiCall'
import { ResponseState, type AdminDeviceSummary, type BaseResponse } from '../api/types'
import { parseWcfDate } from '../api/wcfDate'
import './DataPage.css'

type StatusFilter = 'all' | 'online' | 'offline'
type EnabledFilter = 'all' | 'enabled' | 'disabled'
const PAGE_SIZE = 25

export function DevicesPage() {
  const call = useApiCall()
  const [devices, setDevices] = useState<AdminDeviceSummary[] | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [actionError, setActionError] = useState<string | null>(null)
  const [search, setSearch] = useState('')
  const [statusFilter, setStatusFilter] = useState<StatusFilter>('all')
  const [enabledFilter, setEnabledFilter] = useState<EnabledFilter>('all')
  const [page, setPage] = useState(1)
  const [savingId, setSavingId] = useState<number | null>(null)

  function load() {
    call(() => enumAllDevices())
      .then((res) => {
        if (res.Devices) {
          setDevices(res.Devices)
        } else {
          setError(res.Message || 'Could not load devices')
        }
      })
      .catch(() => setError('Could not reach the server.'))
  }

  useEffect(load, []) // eslint-disable-line react-hooks/exhaustive-deps

  async function runAction(device: AdminDeviceSummary, confirmText: string, action: (deviceId: number) => Promise<BaseResponse>) {
    if (!window.confirm(confirmText)) return
    setActionError(null)
    setSavingId(device.DeviceId)
    try {
      const res = await call(() => action(device.DeviceId))
      if (res.State !== ResponseState.OK) {
        setActionError(res.Message || 'Action failed.')
        return
      }
      load()
    } catch {
      setActionError('Could not reach the server.')
    } finally {
      setSavingId(null)
    }
  }

  const filtered = devices?.filter((d) => {
    const q = search.trim().toLowerCase()
    if (q && !d.Description.toLowerCase().includes(q) && !d.Identifier.toLowerCase().includes(q)) return false
    if (statusFilter === 'online' && !d.IsOnline) return false
    if (statusFilter === 'offline' && d.IsOnline) return false
    if (enabledFilter === 'enabled' && !d.Enabled) return false
    if (enabledFilter === 'disabled' && d.Enabled) return false
    return true
  })

  const totalPages = filtered ? Math.max(1, Math.ceil(filtered.length / PAGE_SIZE)) : 1
  const clampedPage = Math.min(page, totalPages)
  const paged = filtered?.slice((clampedPage - 1) * PAGE_SIZE, clampedPage * PAGE_SIZE)

  function resetToFirstPage() {
    setPage(1)
  }

  return (
    <div>
      <h1>Devices</h1>
      <p className="page-sub">Every panel across the fleet.</p>

      {error && <div className="page-error">{error}</div>}
      {actionError && <div className="page-error">{actionError}</div>}

      {!error && !devices && <div className="page-loading">Loading...</div>}

      {devices && (
        <>
          <div className="filter-row">
            <input
              className="search-input"
              type="search"
              placeholder="Search by description or identifier..."
              value={search}
              onChange={(e) => {
                setSearch(e.target.value)
                resetToFirstPage()
              }}
            />
            <select
              className="role-select"
              value={statusFilter}
              onChange={(e) => {
                setStatusFilter(e.target.value as StatusFilter)
                resetToFirstPage()
              }}
            >
              <option value="all">All status</option>
              <option value="online">Online</option>
              <option value="offline">Offline</option>
            </select>
            <select
              className="role-select"
              value={enabledFilter}
              onChange={(e) => {
                setEnabledFilter(e.target.value as EnabledFilter)
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
                  <th>Description</th>
                  <th>Identifier</th>
                  <th>Status</th>
                  <th>Online</th>
                  <th>Last connection</th>
                  <th>Created</th>
                  <th></th>
                </tr>
              </thead>
              <tbody>
                {paged!.map((d) => (
                  <tr key={d.DeviceId}>
                    <td>
                      <Link className="row-link" to={`/devices/${d.DeviceId}`}>{d.Description}</Link>
                    </td>
                    <td><code>{d.Identifier}</code></td>
                    <td>
                      <span className={d.Enabled ? 'pill ok' : 'pill off'}>{d.Enabled ? 'Enabled' : 'Disabled'}</span>
                    </td>
                    <td>
                      <span className={d.IsOnline ? 'pill ok' : 'pill off'}>{d.IsOnline ? 'Online' : 'Offline'}</span>
                    </td>
                    <td>{parseWcfDate(d.LastConnection)?.toLocaleString() ?? '—'}</td>
                    <td>{parseWcfDate(d.CreatedDateTime)?.toLocaleDateString() ?? '—'}</td>
                    <td>
                      <div className="row-actions">
                        <button
                          type="button"
                          className="action-btn"
                          disabled={savingId === d.DeviceId}
                          onClick={() =>
                            runAction(
                              d,
                              `Block PIN access on "${d.Description}"? This force-logs-out every account linked to it.`,
                              blockDeviceAsAdmin,
                            )
                          }
                        >
                          Block PIN
                        </button>
                        <button
                          type="button"
                          className="action-btn danger"
                          disabled={savingId === d.DeviceId}
                          onClick={() =>
                            runAction(
                              d,
                              `Reset "${d.Description}"? This disables the panel, unlinks every account, and force-logs everyone out. This cannot be undone.`,
                              resetDeviceAsAdmin,
                            )
                          }
                        >
                          Reset PIN
                        </button>
                        <button
                          type="button"
                          className="action-btn danger"
                          disabled={savingId === d.DeviceId}
                          onClick={() =>
                            runAction(
                              d,
                              `Disassociate every account from "${d.Description}"? They'll lose access to this panel.`,
                              disassociateDeviceAsAdmin,
                            )
                          }
                        >
                          Disassociate
                        </button>
                      </div>
                    </td>
                  </tr>
                ))}
                {filtered!.length === 0 && (
                  <tr>
                    <td colSpan={7} className="empty-row">
                      {devices.length === 0 ? 'No devices yet.' : 'No devices match your filters.'}
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
                Page {clampedPage} of {totalPages} &middot; {filtered!.length} device{filtered!.length === 1 ? '' : 's'}
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

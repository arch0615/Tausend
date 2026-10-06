import { useEffect, useState, type FormEvent } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import {
  blockDeviceAsAdmin,
  disassociateDeviceAsAdmin,
  enumAccountDeviceLinks,
  enumAllAccounts,
  enumAllDevices,
  resetDeviceAsAdmin,
  setDeviceEnabled,
  unlinkAccountDevice,
} from '../api/admin'
import { createUsers, enumEvents, enumUsers } from '../api/device'
import {
  getBatteryStatus,
  getFailStatus,
  getGeneralStatus,
  getProgramControls,
  getTime,
  getZones,
  renameZone,
  sendInstallerCommand,
  setExclusions,
  syncTime,
  toggleProgramControl,
} from '../api/command'
import { useApiCall } from '../api/useApiCall'
import {
  ResponseState,
  type AdminAccountSummary,
  type AdminDeviceSummary,
  type AlarmEvent,
  type BaseResponse,
  type BatteryStateResponse,
  type FailStatusResponse,
  type PanelUser,
  type ProgramControl,
  type Zone,
} from '../api/types'
import { parseWcfDate } from '../api/wcfDate'
import './DataPage.css'
import './DeviceDetailPage.css'
import './DeviceUsersPage.css'

const FAULT_LABEL: Record<keyof Omit<FailStatusResponse, 'State' | 'Message' | 'Code'>, string> = {
  AC: 'Main power (220VAC)',
  BAT: 'Battery',
  TLM: 'Phone line',
  BELL1: 'Siren 1',
  BELL2: 'Siren 2',
  VAUX: '12V auxiliary power',
  CLOCK: 'Clock',
  CEL: 'Cellular module',
  COMU: 'Event communication',
  BUS: 'Keypad/accessory bus',
}

const TABS = ['overview', 'diagnostics', 'zones', 'pgm', 'users', 'events', 'installer', 'admin'] as const
type Tab = (typeof TABS)[number]
const TAB_LABEL: Record<Tab, string> = {
  overview: 'Overview',
  diagnostics: 'Diagnostics',
  zones: 'Zones',
  pgm: 'PGM outputs',
  users: 'Panel users',
  events: 'Events',
  installer: 'Installer console',
  admin: 'Administration',
}

export function DeviceDetailPage() {
  const { deviceId: deviceIdParam } = useParams<{ deviceId: string }>()
  const deviceId = Number(deviceIdParam)
  const call = useApiCall()
  const navigate = useNavigate()

  const [tab, setTab] = useState<Tab>('overview')

  const [device, setDevice] = useState<AdminDeviceSummary | null>(null)
  const [linkedAccounts, setLinkedAccounts] = useState<AdminAccountSummary[] | null>(null)
  const [error, setError] = useState<string | null>(null)

  function load() {
    Promise.all([call(() => enumAllDevices()), call(() => enumAllAccounts()), call(() => enumAccountDeviceLinks())])
      .then(([devicesRes, accountsRes, linksRes]) => {
        const found = devicesRes.Devices?.find((d) => d.DeviceId === deviceId) ?? null
        if (!found) {
          setError('Device not found.')
          return
        }
        setDevice(found)
        const accountIds = new Set(
          (linksRes.Links ?? []).filter((l) => l.DeviceId === deviceId).map((l) => l.AccountId),
        )
        setLinkedAccounts((accountsRes.Accounts ?? []).filter((a) => accountIds.has(a.AccountId)))
      })
      .catch(() => setError('Could not reach the server.'))
  }

  useEffect(load, [deviceId]) // eslint-disable-line react-hooks/exhaustive-deps

  return (
    <div>
      <Link className="row-link" to="/devices">&larr; Back to devices</Link>

      {error && <div className="page-error" style={{ marginTop: 12 }}>{error}</div>}
      {!error && !device && <div className="page-loading" style={{ marginTop: 12 }}>Loading...</div>}

      {device && (
        <>
          <h1 style={{ marginTop: 12 }}>{device.Description}</h1>
          <p className="page-sub">Identifier: <code>{device.Identifier}</code></p>

          <div className="tabs">
            {TABS.map((t) => (
              <button key={t} type="button" className={`tab-btn ${tab === t ? 'active' : ''}`} onClick={() => setTab(t)}>
                {TAB_LABEL[t]}
              </button>
            ))}
          </div>

          {tab === 'overview' && <OverviewTab device={device} linkedAccounts={linkedAccounts} />}
          {tab === 'diagnostics' && <DiagnosticsTab deviceId={deviceId} isOnline={device.IsOnline} />}
          {tab === 'zones' && <ZonesTab deviceId={deviceId} />}
          {tab === 'pgm' && <PgmTab deviceId={deviceId} />}
          {tab === 'users' && <UsersTab deviceId={deviceId} />}
          {tab === 'events' && <EventsTab deviceId={deviceId} />}
          {tab === 'installer' && <InstallerTab deviceId={deviceId} />}
          {tab === 'admin' && (
            <AdminTab
              device={device}
              linkedAccounts={linkedAccounts}
              onChanged={() => navigate('/devices')}
              onRefresh={load}
            />
          )}
        </>
      )}
    </div>
  )
}

function OverviewTab({ device, linkedAccounts }: { device: AdminDeviceSummary; linkedAccounts: AdminAccountSummary[] | null }) {
  return (
    <>
      <div className="table-wrap" style={{ marginBottom: 24 }}>
        <table>
          <tbody>
            <tr>
              <th style={{ textAlign: 'left', width: 140 }}>Status</th>
              <td><span className={device.Enabled ? 'pill ok' : 'pill off'}>{device.Enabled ? 'Enabled' : 'Disabled'}</span></td>
            </tr>
            <tr>
              <th style={{ textAlign: 'left' }}>Online</th>
              <td><span className={device.IsOnline ? 'pill ok' : 'pill off'}>{device.IsOnline ? 'Online' : 'Offline'}</span></td>
            </tr>
            <tr>
              <th style={{ textAlign: 'left' }}>Last connection</th>
              <td>{parseWcfDate(device.LastConnection)?.toLocaleString() ?? '—'}</td>
            </tr>
            <tr>
              <th style={{ textAlign: 'left' }}>Created</th>
              <td>{parseWcfDate(device.CreatedDateTime)?.toLocaleDateString() ?? '—'}</td>
            </tr>
          </tbody>
        </table>
      </div>

      <h2 className="section-heading" style={{ marginTop: 0 }}>Linked accounts</h2>
      {linkedAccounts === null ? (
        <div className="page-loading">Loading...</div>
      ) : (
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>Name</th>
                <th>Email</th>
                <th>Role</th>
              </tr>
            </thead>
            <tbody>
              {linkedAccounts.map((a) => (
                <tr key={a.AccountId}>
                  <td><Link className="row-link" to={`/accounts/${a.AccountId}`}>{a.FirstName} {a.LastName}</Link></td>
                  <td>{a.Email}</td>
                  <td><span className={`pill role-${a.Role}`}>{a.Role}</span></td>
                </tr>
              ))}
              {linkedAccounts.length === 0 && (
                <tr>
                  <td colSpan={3} className="empty-row">No accounts have this device linked.</td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      )}
    </>
  )
}

function DiagnosticsTab({ deviceId, isOnline }: { deviceId: number; isOnline: boolean }) {
  const call = useApiCall()
  const [statusText, setStatusText] = useState<string | null>(null)
  const [battery, setBattery] = useState<BatteryStateResponse | null>(null)
  const [faults, setFaults] = useState<FailStatusResponse | null>(null)
  const [liveError, setLiveError] = useState<string | null>(null)
  const [liveLoading, setLiveLoading] = useState(false)

  const [deviceTime, setDeviceTime] = useState<string | null>(null)
  const [serverTime, setServerTime] = useState<string | null>(null)
  const [clockError, setClockError] = useState<string | null>(null)
  const [clockLoading, setClockLoading] = useState(false)
  const [syncing, setSyncing] = useState(false)

  function loadLiveDiagnostics() {
    setLiveLoading(true)
    setLiveError(null)
    Promise.all([call(() => getGeneralStatus(deviceId)), call(() => getBatteryStatus(deviceId)), call(() => getFailStatus(deviceId))])
      .then(([statusRes, batteryRes, faultsRes]) => {
        if (statusRes.State === ResponseState.CENTRAL_UNRESPONSIVE || !statusRes.Text) {
          setLiveError("The panel isn't responding right now -- it may be offline.")
          return
        }
        setStatusText(statusRes.Text)
        setBattery(batteryRes)
        setFaults(faultsRes)
      })
      .catch(() => setLiveError('Could not reach the server.'))
      .finally(() => setLiveLoading(false))
  }

  function loadClock() {
    setClockLoading(true)
    setClockError(null)
    call(() => getTime(deviceId))
      .then((res) => {
        if (res.State !== ResponseState.OK) {
          setClockError(res.Message || "Couldn't read the panel's clock.")
          return
        }
        setDeviceTime(res.DeviceTime)
        setServerTime(res.ServerTime)
      })
      .catch(() => setClockError('Could not reach the server.'))
      .finally(() => setClockLoading(false))
  }

  async function handleSync() {
    if (!window.confirm("Sync this panel's clock to the server's current time?")) return
    setSyncing(true)
    setClockError(null)
    try {
      const res = await call(() => syncTime(deviceId))
      if (res.State !== ResponseState.OK) {
        setClockError(res.Message || 'Sync failed.')
        return
      }
      setDeviceTime(res.DeviceTime)
      setServerTime(res.ServerTime)
    } catch {
      setClockError('Could not reach the server.')
    } finally {
      setSyncing(false)
    }
  }

  useEffect(() => {
    if (isOnline) {
      loadLiveDiagnostics()
      loadClock()
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [deviceId])

  const activeFaults = faults
    ? (Object.keys(FAULT_LABEL) as (keyof typeof FAULT_LABEL)[]).filter((k) => faults[k])
    : []

  return (
    <>
      <div className="live-header">
        <h2 className="section-heading" style={{ margin: 0 }}>Live status</h2>
        <button type="button" className="action-btn" disabled={liveLoading} onClick={loadLiveDiagnostics}>
          {liveLoading ? 'Refreshing...' : 'Refresh'}
        </button>
      </div>

      {!isOnline && !liveLoading && statusText === null && (
        <p className="page-sub">This panel is currently offline -- live diagnostics need it to be reachable. Press Refresh to try anyway.</p>
      )}
      {liveLoading && <div className="page-loading">Querying the panel live -- this can take a few seconds...</div>}
      {liveError && <div className="page-error">{liveError}</div>}

      {!liveLoading && statusText !== null && (
        <div className="live-grid">
          <div className="live-card">
            <div className="live-card-label">Status</div>
            <div className="live-card-value">{statusText || '—'}</div>
          </div>
          {battery && (
            <div className="live-card">
              <div className="live-card-label">Battery</div>
              <div className="live-card-value">
                In: {battery.InTension}V &middot; Charge: {battery.ChargeTension}V &middot; Test: {battery.TestTension}V &middot; Current: {battery.Current}mA
              </div>
            </div>
          )}
          <div className="live-card">
            <div className="live-card-label">Faults</div>
            <div className="live-card-value">
              {activeFaults.length === 0 ? 'None' : activeFaults.map((k) => FAULT_LABEL[k]).join(', ')}
            </div>
          </div>
        </div>
      )}

      <div className="live-header">
        <h2 className="section-heading" style={{ margin: 0 }}>Clock</h2>
        <div className="row-actions">
          <button type="button" className="action-btn" disabled={clockLoading} onClick={loadClock}>
            {clockLoading ? 'Checking...' : 'Refresh'}
          </button>
          <button type="button" className="action-btn" disabled={syncing} onClick={handleSync}>
            {syncing ? 'Syncing...' : 'Sync to server time'}
          </button>
        </div>
      </div>
      {clockError && <div className="page-error">{clockError}</div>}
      {deviceTime && serverTime && (
        <div className="live-grid">
          <div className="live-card">
            <div className="live-card-label">Panel time</div>
            <div className="live-card-value">{new Date(deviceTime).toLocaleString()}</div>
          </div>
          <div className="live-card">
            <div className="live-card-label">Server time</div>
            <div className="live-card-value">{new Date(serverTime).toLocaleString()}</div>
          </div>
        </div>
      )}
    </>
  )
}

function ZonesTab({ deviceId }: { deviceId: number }) {
  const call = useApiCall()
  const [zones, setZones] = useState<Zone[] | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [loading, setLoading] = useState(false)
  const [busyZone, setBusyZone] = useState<number | null>(null)
  const [editingZone, setEditingZone] = useState<number | null>(null)
  const [nameDraft, setNameDraft] = useState('')

  function load() {
    setLoading(true)
    setError(null)
    call(() => getZones(deviceId))
      .then((res) => {
        // Zones themselves come from the DB roster and are always returned even when the panel
        // is offline (CENTRAL_UNRESPONSIVE) -- only Open/Excluded reflect stale/default values in
        // that case. Show the roster either way so renaming still works while offline; just warn
        // that live open/bypass state couldn't be confirmed.
        setZones(res.Zones ?? [])
        if (res.State === ResponseState.CENTRAL_UNRESPONSIVE) {
          setError("The panel isn't responding right now -- open/bypass state below may be stale.")
        }
      })
      .catch(() => setError('Could not reach the server.'))
      .finally(() => setLoading(false))
  }

  useEffect(load, [deviceId]) // eslint-disable-line react-hooks/exhaustive-deps

  function startRename(z: Zone) {
    setEditingZone(z.ZoneNumber)
    setNameDraft(z.Name ?? '')
  }

  async function saveRename(z: Zone) {
    if (!nameDraft.trim()) return
    setBusyZone(z.ZoneNumber)
    setError(null)
    try {
      const res = await call(() => renameZone(deviceId, z.ZoneNumber, nameDraft.trim()))
      if (res.State !== ResponseState.OK) {
        setError(res.Message || 'Rename failed.')
        return
      }
      setZones((prev) => prev?.map((x) => (x.ZoneNumber === z.ZoneNumber ? { ...x, Name: nameDraft.trim() } : x)) ?? null)
      setEditingZone(null)
    } catch {
      setError('Could not reach the server.')
    } finally {
      setBusyZone(null)
    }
  }

  async function toggleExclusion(z: Zone) {
    if (!zones) return
    const nowExcluded = !z.Excluded
    const desired = zones.filter((x) => (x.ZoneNumber === z.ZoneNumber ? nowExcluded : x.Excluded)).map((x) => x.ZoneNumber)
    setBusyZone(z.ZoneNumber)
    setError(null)
    try {
      const res = await call(() => setExclusions(deviceId, desired))
      if (res.State === ResponseState.CENTRAL_UNRESPONSIVE) {
        setError("The panel isn't responding right now -- it may be offline.")
        return
      }
      if (res.State !== ResponseState.OK) {
        setError(res.Message || 'Could not update bypass state.')
        return
      }
      setZones((prev) => prev?.map((x) => (x.ZoneNumber === z.ZoneNumber ? { ...x, Excluded: nowExcluded } : x)) ?? null)
    } catch {
      setError('Could not reach the server.')
    } finally {
      setBusyZone(null)
    }
  }

  return (
    <>
      <div className="live-header">
        <h2 className="section-heading" style={{ margin: 0 }}>Zones</h2>
        <button type="button" className="action-btn" disabled={loading} onClick={load}>
          {loading ? 'Refreshing...' : 'Refresh'}
        </button>
      </div>
      {error && <div className="page-error">{error}</div>}
      {loading && !zones && <div className="page-loading">Querying the panel live -- this can take a few seconds...</div>}

      {zones && (
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>#</th>
                <th>Name</th>
                <th>Open</th>
                <th>Bypassed</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {zones.map((z) => (
                <tr key={z.ZoneNumber}>
                  <td>{z.ZoneNumber}</td>
                  <td>
                    {editingZone === z.ZoneNumber ? (
                      <span className="inline-form">
                        <input
                          className="inline-input"
                          value={nameDraft}
                          onChange={(e) => setNameDraft(e.target.value)}
                          autoFocus
                        />
                        <button type="button" className="action-btn" disabled={busyZone === z.ZoneNumber} onClick={() => saveRename(z)}>
                          Save
                        </button>
                        <button type="button" className="action-btn" onClick={() => setEditingZone(null)}>
                          Cancel
                        </button>
                      </span>
                    ) : (
                      <button type="button" className="link-btn" onClick={() => startRename(z)}>
                        {z.Name || `Zone ${z.ZoneNumber}`}
                      </button>
                    )}
                  </td>
                  <td><span className={z.Open ? 'pill off' : 'pill ok'}>{z.Open ? 'Open' : 'Closed'}</span></td>
                  <td><span className={z.Excluded ? 'pill off' : 'pill ok'}>{z.Excluded ? 'Bypassed' : 'Active'}</span></td>
                  <td>
                    <button
                      type="button"
                      className="action-btn"
                      disabled={busyZone === z.ZoneNumber}
                      onClick={() => toggleExclusion(z)}
                    >
                      {z.Excluded ? 'Restore' : 'Bypass'}
                    </button>
                  </td>
                </tr>
              ))}
              {zones.length === 0 && (
                <tr>
                  <td colSpan={5} className="empty-row">No zones configured.</td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      )}
    </>
  )
}

function PgmTab({ deviceId }: { deviceId: number }) {
  const call = useApiCall()
  const [controls, setControls] = useState<ProgramControl[] | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [loading, setLoading] = useState(false)
  const [busy, setBusy] = useState<number | null>(null)

  function load() {
    setLoading(true)
    setError(null)
    call(() => getProgramControls(deviceId))
      .then((res) => {
        if (res.State !== ResponseState.OK) {
          setError(res.Message || "Couldn't load PGM outputs.")
          return
        }
        setControls(res.ProgramControls ?? [])
      })
      .catch(() => setError('Could not reach the server.'))
      .finally(() => setLoading(false))
  }

  useEffect(load, [deviceId]) // eslint-disable-line react-hooks/exhaustive-deps

  async function toggle(pc: ProgramControl) {
    setBusy(pc.ProgramControlNumber)
    setError(null)
    try {
      const res = await call(() => toggleProgramControl(deviceId, pc.ProgramControlNumber, !pc.Activated))
      if (res.State !== ResponseState.OK) {
        setError(res.Message || 'Toggle failed.')
        return
      }
      const updated = res.ProgramControls?.find((x) => x.ProgramControlNumber === pc.ProgramControlNumber)
      setControls((prev) =>
        prev?.map((x) => (x.ProgramControlNumber === pc.ProgramControlNumber ? updated ?? { ...x, Activated: !x.Activated } : x)) ?? null,
      )
    } catch {
      setError('Could not reach the server.')
    } finally {
      setBusy(null)
    }
  }

  return (
    <>
      <div className="live-header">
        <h2 className="section-heading" style={{ margin: 0 }}>PGM outputs</h2>
        <button type="button" className="action-btn" disabled={loading} onClick={load}>
          {loading ? 'Refreshing...' : 'Refresh'}
        </button>
      </div>
      {error && <div className="page-error">{error}</div>}
      {loading && !controls && <div className="page-loading">Querying the panel live -- this can take a few seconds...</div>}

      {controls && (
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>#</th>
                <th>Name</th>
                <th>State</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {controls.map((pc) => (
                <tr key={pc.ProgramControlNumber}>
                  <td>{pc.ProgramControlNumber}</td>
                  <td>{pc.Name || `Output ${pc.ProgramControlNumber}`}</td>
                  <td><span className={pc.Activated ? 'pill ok' : 'pill off'}>{pc.Activated ? 'On' : 'Off'}</span></td>
                  <td>
                    <button type="button" className="action-btn" disabled={busy === pc.ProgramControlNumber} onClick={() => toggle(pc)}>
                      {pc.Activated ? 'Turn off' : 'Turn on'}
                    </button>
                  </td>
                </tr>
              ))}
              {controls.length === 0 && (
                <tr>
                  <td colSpan={4} className="empty-row">No PGM outputs configured.</td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      )}
    </>
  )
}

function UsersTab({ deviceId }: { deviceId: number }) {
  const call = useApiCall()
  const [users, setUsers] = useState<PanelUser[] | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [userNumber, setUserNumber] = useState('')
  const [userName, setUserName] = useState('')
  const [saving, setSaving] = useState(false)
  const [saveError, setSaveError] = useState<string | null>(null)

  function load() {
    call(() => enumUsers(deviceId))
      .then((res) => setUsers(res.Users ?? []))
      .catch(() => setError('Could not reach the server.'))
  }

  useEffect(load, [deviceId]) // eslint-disable-line react-hooks/exhaustive-deps

  async function handleSave(e: FormEvent) {
    e.preventDefault()
    const number = Number(userNumber)
    if (!Number.isInteger(number) || number < 0 || !userName.trim()) {
      setSaveError('Enter a valid user number and a name.')
      return
    }
    setSaving(true)
    setSaveError(null)
    try {
      const res = await call(() => createUsers(deviceId, [{ UserNumber: number, UserName: userName.trim() }]))
      if (res.State !== ResponseState.OK) {
        setSaveError(res.Message || 'Could not save.')
        return
      }
      setUserNumber('')
      setUserName('')
      load()
    } catch {
      setSaveError('Could not reach the server.')
    } finally {
      setSaving(false)
    }
  }

  function editUser(u: PanelUser) {
    setUserNumber(String(u.UserNumber))
    setUserName(u.UserName)
  }

  return (
    <>
      {error && <div className="page-error">{error}</div>}
      {!error && !users && <div className="page-loading">Loading...</div>}

      {users && (
        <>
          <div className="table-wrap" style={{ marginBottom: 20 }}>
            <table>
              <thead>
                <tr>
                  <th>User #</th>
                  <th>Name</th>
                  <th></th>
                </tr>
              </thead>
              <tbody>
                {users.map((u) => (
                  <tr key={u.UserId}>
                    <td>{u.UserNumber}</td>
                    <td>{u.UserName}</td>
                    <td>
                      <button type="button" className="link-btn" onClick={() => editUser(u)}>
                        Edit
                      </button>
                    </td>
                  </tr>
                ))}
                {users.length === 0 && (
                  <tr>
                    <td colSpan={3} className="empty-row">No user labels set for this panel yet.</td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>

          <form className="user-form" onSubmit={handleSave}>
            <h2 style={{ marginTop: 0 }}>Add / edit a user</h2>
            <label>
              User number
              <input type="number" min={0} value={userNumber} onChange={(e) => setUserNumber(e.target.value)} required />
            </label>
            <label>
              Name
              <input type="text" value={userName} onChange={(e) => setUserName(e.target.value)} required />
            </label>
            {saveError && <div className="page-error">{saveError}</div>}
            <button type="submit" disabled={saving}>
              {saving ? 'Saving...' : 'Save'}
            </button>
          </form>
        </>
      )}
    </>
  )
}

function EventsTab({ deviceId }: { deviceId: number }) {
  const call = useApiCall()
  const [events, setEvents] = useState<AlarmEvent[] | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [loading, setLoading] = useState(false)

  function load() {
    setLoading(true)
    setError(null)
    call(() => enumEvents(deviceId))
      .then((res) => setEvents(res.Events ?? []))
      .catch(() => setError('Could not reach the server.'))
      .finally(() => setLoading(false))
  }

  useEffect(load, [deviceId]) // eslint-disable-line react-hooks/exhaustive-deps

  return (
    <>
      <div className="live-header">
        <h2 className="section-heading" style={{ margin: 0 }}>Event history</h2>
        <button type="button" className="action-btn" disabled={loading} onClick={load}>
          {loading ? 'Refreshing...' : 'Refresh'}
        </button>
      </div>
      {error && <div className="page-error">{error}</div>}
      {loading && !events && <div className="page-loading">Loading...</div>}
      {events && (
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>When</th>
                <th>Type</th>
                <th>Details</th>
              </tr>
            </thead>
            <tbody>
              {events.map((e) => (
                <tr key={e.EventId}>
                  <td>{parseWcfDate(e.EventDateTime)?.toLocaleString() ?? e.StringDate ?? '—'}</td>
                  <td>
                    <span className={`pill ${e.EventType === 'R' ? 'ok' : 'off'}`}>{e.EventType === 'R' ? 'Restore' : 'Event'}</span>
                    {' '}#{e.NotificationType}
                  </td>
                  <td>{e.Text || '—'}</td>
                </tr>
              ))}
              {events.length === 0 && (
                <tr>
                  <td colSpan={3} className="empty-row">No events logged for this panel yet.</td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      )}
    </>
  )
}

function InstallerTab({ deviceId }: { deviceId: number }) {
  const call = useApiCall()
  const [command, setCommand] = useState('')
  const [sending, setSending] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [history, setHistory] = useState<{ command: string; response: string; at: Date }[]>([])

  async function handleSend(e: FormEvent) {
    e.preventDefault()
    if (!command.trim()) return
    if (!window.confirm(`Send raw command "${command.trim()}" directly to this panel? This bypasses every normal safety check.`)) {
      return
    }
    setError(null)
    setSending(true)
    try {
      const res = await call(() => sendInstallerCommand(deviceId, command.trim()))
      setHistory((prev) => [{ command: command.trim(), response: res.Text || res.Message || '(empty response)', at: new Date() }, ...prev])
      setCommand('')
    } catch {
      setError('Could not reach the server.')
    } finally {
      setSending(false)
    }
  }

  return (
    <>
      <p className="page-sub">
        Sends a raw command directly to the panel through the relay. Every command is recorded in
        the audit log regardless of outcome. Admin/installer only -- there is no undo.
      </p>
      {error && <div className="page-error">{error}</div>}
      <form className="inline-form" onSubmit={handleSend} style={{ marginBottom: 20 }}>
        <input
          className="inline-input"
          style={{ width: 280 }}
          value={command}
          onChange={(e) => setCommand(e.target.value)}
          placeholder="Raw command..."
          disabled={sending}
        />
        <button type="submit" className="action-btn danger" disabled={sending || !command.trim()}>
          {sending ? 'Sending...' : 'Send'}
        </button>
      </form>

      <h2 className="section-heading" style={{ marginTop: 0 }}>Session history</h2>
      {history.length === 0 ? (
        <p className="page-sub">No commands sent this session.</p>
      ) : (
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>When</th>
                <th>Command</th>
                <th>Response</th>
              </tr>
            </thead>
            <tbody>
              {history.map((h, i) => (
                <tr key={i}>
                  <td>{h.at.toLocaleTimeString()}</td>
                  <td><code>{h.command}</code></td>
                  <td>{h.response}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </>
  )
}

function AdminTab({
  device,
  linkedAccounts,
  onChanged,
  onRefresh,
}: {
  device: AdminDeviceSummary
  linkedAccounts: AdminAccountSummary[] | null
  onChanged: () => void
  onRefresh: () => void
}) {
  const call = useApiCall()
  const [actionError, setActionError] = useState<string | null>(null)
  const [actionPending, setActionPending] = useState(false)
  const [unlinkingId, setUnlinkingId] = useState<number | null>(null)
  const [togglingAccess, setTogglingAccess] = useState(false)

  async function runAction(confirmText: string, action: (deviceId: number) => Promise<BaseResponse>) {
    if (!window.confirm(confirmText)) return
    setActionError(null)
    setActionPending(true)
    try {
      const res = await call(() => action(device.DeviceId))
      if (res.State !== ResponseState.OK) {
        setActionError(res.Message || 'Action failed.')
        return
      }
      onChanged()
    } catch {
      setActionError('Could not reach the server.')
    } finally {
      setActionPending(false)
    }
  }

  async function handleToggleAccess() {
    const next = !device.Enabled
    const confirmText = next
      ? `Unblock access to "${device.Description}"? This re-enables the panel (existing linked accounts, if any, are unaffected).`
      : `Disable access to "${device.Description}"? This blocks the panel without unlinking accounts or resetting pairing.`
    if (!window.confirm(confirmText)) return
    setActionError(null)
    setTogglingAccess(true)
    try {
      const res = await call(() => setDeviceEnabled(device.DeviceId, next))
      if (res.State !== ResponseState.OK) {
        setActionError(res.Message || 'Could not update device access.')
        return
      }
      onRefresh()
    } catch {
      setActionError('Could not reach the server.')
    } finally {
      setTogglingAccess(false)
    }
  }

  async function handleUnlink(account: AdminAccountSummary) {
    if (!window.confirm(`Unlink ${account.Email} from "${device.Description}"? They'll lose access to this panel.`)) return
    setActionError(null)
    setUnlinkingId(account.AccountId)
    try {
      const res = await call(() => unlinkAccountDevice(account.AccountId, device.DeviceId))
      if (res.State !== ResponseState.OK) {
        setActionError(res.Message || 'Could not unlink account.')
        return
      }
      onRefresh()
    } catch {
      setActionError('Could not reach the server.')
    } finally {
      setUnlinkingId(null)
    }
  }

  return (
    <>
      <p className="page-sub">Fleet-wide admin actions on this panel. These affect every account linked to it.</p>
      {actionError && <div className="page-error">{actionError}</div>}
      <div className="row-actions" style={{ marginBottom: 24 }}>
        <button type="button" className={device.Enabled ? 'action-btn danger' : 'action-btn'} disabled={togglingAccess} onClick={handleToggleAccess}>
          {device.Enabled ? 'Disable access' : 'Unblock access'}
        </button>
        <button
          type="button"
          className="action-btn"
          disabled={actionPending}
          onClick={() => runAction(`Block PIN access on "${device.Description}"? This force-logs-out every account linked to it.`, blockDeviceAsAdmin)}
        >
          Block PIN
        </button>
        <button
          type="button"
          className="action-btn danger"
          disabled={actionPending}
          onClick={() =>
            runAction(
              `Reset "${device.Description}"? This disables the panel, unlinks every account, and force-logs everyone out. This cannot be undone.`,
              resetDeviceAsAdmin,
            )
          }
        >
          Reset PIN
        </button>
        <button
          type="button"
          className="action-btn danger"
          disabled={actionPending}
          onClick={() => runAction(`Disassociate every account from "${device.Description}"? They'll lose access to this panel.`, disassociateDeviceAsAdmin)}
        >
          Disassociate all
        </button>
      </div>

      <h2 className="section-heading" style={{ marginTop: 0 }}>Panel ownership</h2>
      {linkedAccounts === null ? (
        <div className="page-loading">Loading...</div>
      ) : (
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>Name</th>
                <th>Email</th>
                <th>Role</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {linkedAccounts.map((a) => (
                <tr key={a.AccountId}>
                  <td><Link className="row-link" to={`/accounts/${a.AccountId}`}>{a.FirstName} {a.LastName}</Link></td>
                  <td>{a.Email}</td>
                  <td><span className={`pill role-${a.Role}`}>{a.Role}</span></td>
                  <td>
                    <button
                      type="button"
                      className="action-btn danger"
                      disabled={unlinkingId === a.AccountId}
                      onClick={() => handleUnlink(a)}
                    >
                      Unlink
                    </button>
                  </td>
                </tr>
              ))}
              {linkedAccounts.length === 0 && (
                <tr>
                  <td colSpan={4} className="empty-row">No accounts have this device linked.</td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      )}
    </>
  )
}

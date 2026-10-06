import { useEffect, useState } from 'react'
import { enumAllAccounts, enumAllDevices } from '../api/admin'
import { useApiCall } from '../api/useApiCall'
import { AccountRole, type AdminAccountSummary, type AdminDeviceSummary } from '../api/types'
import './DataPage.css'
import './DeviceDetailPage.css'

export function SystemStatusPage() {
  const call = useApiCall()
  const [devices, setDevices] = useState<AdminDeviceSummary[] | null>(null)
  const [accounts, setAccounts] = useState<AdminAccountSummary[] | null>(null)
  const [backendUp, setBackendUp] = useState<boolean | null>(null)

  useEffect(() => {
    Promise.all([call(() => enumAllDevices()), call(() => enumAllAccounts())])
      .then(([devicesRes, accountsRes]) => {
        setBackendUp(true)
        setDevices(devicesRes.Devices ?? [])
        setAccounts(accountsRes.Accounts ?? [])
      })
      .catch(() => setBackendUp(false))
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  const online = devices?.filter((d) => d.IsOnline).length ?? 0
  const offline = devices ? devices.length - online : 0
  const enabled = devices?.filter((d) => d.Enabled).length ?? 0
  const disabled = devices ? devices.length - enabled : 0
  const admins = accounts?.filter((a) => a.Role === AccountRole.Admin).length ?? 0
  const installers = accounts?.filter((a) => a.Role === AccountRole.Installer).length ?? 0
  const endUsers = accounts?.filter((a) => a.Role === AccountRole.EndUser).length ?? 0
  const disabledAccounts = accounts?.filter((a) => !a.Enabled).length ?? 0

  return (
    <div>
      <h1>System status</h1>
      <p className="page-sub">
        A live snapshot, not a monitoring dashboard -- this reflects the data available from the
        admin API right now, nothing tracked over time.
      </p>

      <h2 className="section-heading" style={{ marginTop: 0 }}>Backend</h2>
      <div className="live-grid" style={{ marginBottom: 24 }}>
        <div className="live-card">
          <div className="live-card-label">API &amp; database</div>
          <div className="live-card-value">
            {backendUp === null && 'Checking...'}
            {backendUp === true && <span className="pill ok">Reachable</span>}
            {backendUp === false && <span className="pill off">Unreachable</span>}
          </div>
        </div>
      </div>

      {devices && (
        <>
          <h2 className="section-heading">Panels ({devices.length} total)</h2>
          <div className="live-grid" style={{ marginBottom: 24 }}>
            <div className="live-card">
              <div className="live-card-label">Online / offline</div>
              <div className="live-card-value">{online} online &middot; {offline} offline</div>
            </div>
            <div className="live-card">
              <div className="live-card-label">Access</div>
              <div className="live-card-value">{enabled} enabled &middot; {disabled} disabled</div>
            </div>
          </div>
        </>
      )}

      {accounts && (
        <>
          <h2 className="section-heading">Accounts ({accounts.length} total)</h2>
          <div className="live-grid">
            <div className="live-card">
              <div className="live-card-label">By role</div>
              <div className="live-card-value">{endUsers} end user &middot; {installers} installer &middot; {admins} admin</div>
            </div>
            <div className="live-card">
              <div className="live-card-label">Disabled</div>
              <div className="live-card-value">{disabledAccounts}</div>
            </div>
          </div>
        </>
      )}

      <p className="page-sub" style={{ marginTop: 24 }}>
        Not tracked yet: SMS gateway status (SMS panels aren't managed here), failed-communication
        history, and error logs -- there's no data source for those in the backend today.
      </p>
    </div>
  )
}

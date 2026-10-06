import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { enumAllAccounts, enumAllDevices, enumAuditLog } from '../api/admin'
import { useApiCall } from '../api/useApiCall'
import { AccountRole, type AdminAccountSummary, type AdminDeviceSummary, type AuditLogEntry } from '../api/types'
import { parseWcfDate } from '../api/wcfDate'
import './OverviewPage.css'

const ACTION_LABEL: Record<string, string> = {
  SetAccountRole: 'Changed role',
  BlockDevice: 'Blocked PIN',
  ResetDevice: 'Reset PIN',
  DisassociateDevice: 'Disassociated',
}

export function OverviewPage() {
  const call = useApiCall()
  const [accounts, setAccounts] = useState<AdminAccountSummary[] | null>(null)
  const [devices, setDevices] = useState<AdminDeviceSummary[] | null>(null)
  const [auditEntries, setAuditEntries] = useState<AuditLogEntry[] | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    Promise.all([call(() => enumAllAccounts()), call(() => enumAllDevices()), call(() => enumAuditLog())])
      .then(([accountsRes, devicesRes, auditRes]) => {
        if (accountsRes.Accounts) setAccounts(accountsRes.Accounts)
        if (devicesRes.Devices) setDevices(devicesRes.Devices)
        if (auditRes.Entries) setAuditEntries(auditRes.Entries)
        if (!accountsRes.Accounts && !devicesRes.Devices) {
          setError(accountsRes.Message || devicesRes.Message || 'Could not load the overview')
        }
      })
      .catch(() => setError('Could not reach the server.'))
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  const loading = !error && (accounts === null || devices === null)

  const accountsByRole = accounts?.reduce(
    (acc, a) => {
      acc[a.Role] = (acc[a.Role] ?? 0) + 1
      return acc
    },
    {} as Record<AccountRole, number>,
  )

  const onlineDevices = devices?.filter((d) => d.IsOnline).length ?? 0
  const disabledDevices = devices?.filter((d) => !d.Enabled).length ?? 0

  return (
    <div>
      <h1>Overview</h1>
      <p className="page-sub">Fleet-wide status at a glance.</p>

      {error && <div className="page-error">{error}</div>}
      {loading && <div className="page-loading">Loading...</div>}

      {!loading && !error && (
        <>
          <div className="kpi-grid">
            <Link to="/accounts" className="kpi-card">
              <div className="kpi-value">{accounts!.length}</div>
              <div className="kpi-label">Total accounts</div>
              <div className="kpi-detail">
                {accountsByRole?.[AccountRole.EndUser] ?? 0} end users &middot;{' '}
                {accountsByRole?.[AccountRole.Installer] ?? 0} installers &middot;{' '}
                {accountsByRole?.[AccountRole.Admin] ?? 0} admins
              </div>
            </Link>
            <Link to="/devices" className="kpi-card">
              <div className="kpi-value">{devices!.length}</div>
              <div className="kpi-label">Total devices</div>
              <div className="kpi-detail">
                <span className="kpi-ok">{onlineDevices} online</span> &middot;{' '}
                {devices!.length - onlineDevices} offline
              </div>
            </Link>
            <Link to="/devices" className="kpi-card">
              <div className="kpi-value">{disabledDevices}</div>
              <div className="kpi-label">Disabled devices</div>
              <div className="kpi-detail">Blocked or reset panels</div>
            </Link>
            <Link to="/audit-log" className="kpi-card">
              <div className="kpi-value">{auditEntries?.length ?? '—'}</div>
              <div className="kpi-label">Recent admin actions</div>
              <div className="kpi-detail">Last 200, most recent first</div>
            </Link>
          </div>

          <h2 className="section-heading">Recent activity</h2>
          {auditEntries === null ? (
            <div className="page-loading">Loading...</div>
          ) : (
            <div className="table-wrap">
              <table>
                <thead>
                  <tr>
                    <th>When</th>
                    <th>Actor</th>
                    <th>Action</th>
                    <th>Target</th>
                  </tr>
                </thead>
                <tbody>
                  {auditEntries.slice(0, 10).map((e) => (
                    <tr key={e.AuditLogId}>
                      <td>{parseWcfDate(e.CreatedDateTime)?.toLocaleString() ?? '—'}</td>
                      <td>{e.ActorEmail ?? `Account #${e.ActorAccountId}`}</td>
                      <td>{ACTION_LABEL[e.Action] ?? e.Action}</td>
                      <td>{e.TargetType}{e.TargetId != null ? ` #${e.TargetId}` : ''}</td>
                    </tr>
                  ))}
                  {auditEntries.length === 0 && (
                    <tr>
                      <td colSpan={4} className="empty-row">No admin actions logged yet.</td>
                    </tr>
                  )}
                </tbody>
              </table>
              {auditEntries.length > 10 && (
                <Link className="row-link" to="/audit-log" style={{ display: 'inline-block', marginTop: 12 }}>
                  View full audit log &rarr;
                </Link>
              )}
            </div>
          )}
        </>
      )}
    </div>
  )
}

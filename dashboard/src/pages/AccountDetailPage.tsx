import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import {
  enumAccountDeviceLinks,
  enumAllAccounts,
  enumAllDevices,
  revokeAccountSessions,
  setAccountEnabled,
  setAccountRole,
  unlinkAccountDevice,
} from '../api/admin'
import { useApiCall } from '../api/useApiCall'
import { AccountRole, ResponseState, type AdminAccountSummary, type AdminDeviceSummary } from '../api/types'
import { parseWcfDate } from '../api/wcfDate'
import './DataPage.css'

const ROLE_LABEL: Record<AccountRole, string> = {
  [AccountRole.EndUser]: 'End user',
  [AccountRole.Installer]: 'Installer',
  [AccountRole.Admin]: 'Admin',
}

const ROLE_OPTIONS = [AccountRole.EndUser, AccountRole.Installer, AccountRole.Admin]

export function AccountDetailPage() {
  const { accountId: accountIdParam } = useParams<{ accountId: string }>()
  const accountId = Number(accountIdParam)
  const call = useApiCall()

  const [account, setAccount] = useState<AdminAccountSummary | null>(null)
  const [linkedDevices, setLinkedDevices] = useState<AdminDeviceSummary[] | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [actionError, setActionError] = useState<string | null>(null)
  const [actionMessage, setActionMessage] = useState<string | null>(null)
  const [savingRole, setSavingRole] = useState(false)
  const [savingStatus, setSavingStatus] = useState(false)
  const [revoking, setRevoking] = useState(false)
  const [unlinkingId, setUnlinkingId] = useState<number | null>(null)

  function load() {
    Promise.all([call(() => enumAllAccounts()), call(() => enumAllDevices()), call(() => enumAccountDeviceLinks())])
      .then(([accountsRes, devicesRes, linksRes]) => {
        const found = accountsRes.Accounts?.find((a) => a.AccountId === accountId) ?? null
        if (!found) {
          setError('Account not found.')
          return
        }
        setAccount(found)
        const deviceIds = new Set(
          (linksRes.Links ?? []).filter((l) => l.AccountId === accountId).map((l) => l.DeviceId),
        )
        setLinkedDevices((devicesRes.Devices ?? []).filter((d) => deviceIds.has(d.DeviceId)))
      })
      .catch(() => setError('Could not reach the server.'))
  }

  useEffect(load, [accountId]) // eslint-disable-line react-hooks/exhaustive-deps

  async function handleRoleChange(role: AccountRole) {
    if (!account || role === account.Role) return
    if (!window.confirm(`Change ${account.Email}'s role from ${ROLE_LABEL[account.Role]} to ${ROLE_LABEL[role]}?`)) {
      return
    }
    setActionError(null)
    setSavingRole(true)
    try {
      const res = await call(() => setAccountRole(account.AccountId, role))
      if (res.State !== ResponseState.OK) {
        setActionError(res.Message || 'Could not update role.')
        return
      }
      setAccount({ ...account, Role: role })
    } catch {
      setActionError('Could not reach the server.')
    } finally {
      setSavingRole(false)
    }
  }

  async function handleToggleEnabled() {
    if (!account) return
    const next = !account.Enabled
    const confirmText = next
      ? `Enable ${account.Email}? They'll be able to log in again.`
      : `Disable ${account.Email}? This force-logs them out of every active session and blocks further logins.`
    if (!window.confirm(confirmText)) return
    setActionError(null)
    setActionMessage(null)
    setSavingStatus(true)
    try {
      const res = await call(() => setAccountEnabled(account.AccountId, next))
      if (res.State !== ResponseState.OK) {
        setActionError(res.Message || 'Could not update account status.')
        return
      }
      setAccount({ ...account, Enabled: next })
    } catch {
      setActionError('Could not reach the server.')
    } finally {
      setSavingStatus(false)
    }
  }

  async function handleRevokeSessions() {
    if (!account) return
    if (!window.confirm(`Revoke every active session for ${account.Email}? They'll be logged out everywhere and need to sign in again.`)) {
      return
    }
    setActionError(null)
    setActionMessage(null)
    setRevoking(true)
    try {
      const res = await call(() => revokeAccountSessions(account.AccountId))
      if (res.State !== ResponseState.OK) {
        setActionError(res.Message || 'Could not revoke sessions.')
        return
      }
      setActionMessage('Sessions revoked.')
    } catch {
      setActionError('Could not reach the server.')
    } finally {
      setRevoking(false)
    }
  }

  async function handleUnlink(device: AdminDeviceSummary) {
    if (!account) return
    if (!window.confirm(`Unlink "${device.Description}" from ${account.Email}? They'll lose access to this panel.`)) return
    setActionError(null)
    setUnlinkingId(device.DeviceId)
    try {
      const res = await call(() => unlinkAccountDevice(account.AccountId, device.DeviceId))
      if (res.State !== ResponseState.OK) {
        setActionError(res.Message || 'Could not unlink device.')
        return
      }
      setLinkedDevices((prev) => prev?.filter((d) => d.DeviceId !== device.DeviceId) ?? null)
    } catch {
      setActionError('Could not reach the server.')
    } finally {
      setUnlinkingId(null)
    }
  }

  return (
    <div>
      <Link className="row-link" to="/accounts">&larr; Back to accounts</Link>

      {error && <div className="page-error" style={{ marginTop: 12 }}>{error}</div>}
      {!error && !account && <div className="page-loading" style={{ marginTop: 12 }}>Loading...</div>}

      {account && (
        <>
          <h1 style={{ marginTop: 12 }}>{account.FirstName} {account.LastName}</h1>
          <p className="page-sub">{account.Email}</p>

          {actionError && <div className="page-error">{actionError}</div>}
          {actionMessage && <div className="page-sub">{actionMessage}</div>}

          <div className="table-wrap" style={{ marginBottom: 20 }}>
            <table>
              <tbody>
                <tr>
                  <th style={{ textAlign: 'left', width: 140 }}>Role</th>
                  <td>
                    <span className={`pill role-${account.Role}`}>{ROLE_LABEL[account.Role]}</span>
                    <select
                      className="role-select"
                      value={account.Role}
                      disabled={savingRole}
                      onChange={(e) => handleRoleChange(Number(e.target.value) as AccountRole)}
                    >
                      {ROLE_OPTIONS.map((r) => (
                        <option key={r} value={r}>{ROLE_LABEL[r]}</option>
                      ))}
                    </select>
                  </td>
                </tr>
                <tr>
                  <th style={{ textAlign: 'left' }}>Status</th>
                  <td><span className={account.Enabled ? 'pill ok' : 'pill off'}>{account.Enabled ? 'Enabled' : 'Disabled'}</span></td>
                </tr>
                <tr>
                  <th style={{ textAlign: 'left' }}>Created</th>
                  <td>{parseWcfDate(account.CreatedDateTime)?.toLocaleString() ?? '—'}</td>
                </tr>
                <tr>
                  <th style={{ textAlign: 'left' }}>Last login</th>
                  <td>{parseWcfDate(account.LastLoginDateTime)?.toLocaleString() ?? 'Never'}</td>
                </tr>
              </tbody>
            </table>
          </div>

          <div className="row-actions" style={{ marginBottom: 24 }}>
            <button
              type="button"
              className={account.Enabled ? 'action-btn danger' : 'action-btn'}
              disabled={savingStatus}
              onClick={handleToggleEnabled}
            >
              {account.Enabled ? 'Disable account' : 'Enable account'}
            </button>
            <button type="button" className="action-btn danger" disabled={revoking} onClick={handleRevokeSessions}>
              {revoking ? 'Revoking...' : 'Revoke active sessions'}
            </button>
          </div>

          <h2 className="section-heading" style={{ marginTop: 0 }}>Linked devices</h2>
          {linkedDevices === null ? (
            <div className="page-loading">Loading...</div>
          ) : (
            <div className="table-wrap">
              <table>
                <thead>
                  <tr>
                    <th>Description</th>
                    <th>Identifier</th>
                    <th>Status</th>
                    <th>Online</th>
                    <th></th>
                  </tr>
                </thead>
                <tbody>
                  {linkedDevices.map((d) => (
                    <tr key={d.DeviceId}>
                      <td><Link className="row-link" to={`/devices/${d.DeviceId}`}>{d.Description}</Link></td>
                      <td><code>{d.Identifier}</code></td>
                      <td><span className={d.Enabled ? 'pill ok' : 'pill off'}>{d.Enabled ? 'Enabled' : 'Disabled'}</span></td>
                      <td><span className={d.IsOnline ? 'pill ok' : 'pill off'}>{d.IsOnline ? 'Online' : 'Offline'}</span></td>
                      <td>
                        <button
                          type="button"
                          className="action-btn danger"
                          disabled={unlinkingId === d.DeviceId}
                          onClick={() => handleUnlink(d)}
                        >
                          Unlink
                        </button>
                      </td>
                    </tr>
                  ))}
                  {linkedDevices.length === 0 && (
                    <tr>
                      <td colSpan={5} className="empty-row">This account has no devices linked.</td>
                    </tr>
                  )}
                </tbody>
              </table>
            </div>
          )}
        </>
      )}
    </div>
  )
}

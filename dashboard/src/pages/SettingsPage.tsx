import { useState, type FormEvent } from 'react'
import { updateOwnPassword } from '../api/auth'
import { useAuth } from '../auth/AuthContext'
import { ResponseState } from '../api/types'
import './DataPage.css'
import './DeviceUsersPage.css'

export function SettingsPage() {
  const { session } = useAuth()
  const [oldPassword, setOldPassword] = useState('')
  const [newPassword, setNewPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [success, setSuccess] = useState(false)

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    setError(null)
    setSuccess(false)
    if (newPassword !== confirmPassword) {
      setError('New password and confirmation do not match.')
      return
    }
    if (newPassword.length < 6) {
      setError('New password must be at least 6 characters.')
      return
    }
    setSaving(true)
    try {
      const res = await updateOwnPassword(oldPassword, newPassword)
      if (res.State !== ResponseState.OK) {
        setError(res.Message || 'Could not change password.')
        return
      }
      setSuccess(true)
      setOldPassword('')
      setNewPassword('')
      setConfirmPassword('')
    } catch {
      setError('Could not reach the server.')
    } finally {
      setSaving(false)
    }
  }

  return (
    <div>
      <h1>Settings</h1>
      <p className="page-sub">Signed in as {session?.account.Email}.</p>

      <h2 className="section-heading" style={{ marginTop: 0 }}>Change password</h2>
      <form className="user-form" onSubmit={handleSubmit}>
        <label>
          Current password
          <input type="password" value={oldPassword} onChange={(e) => setOldPassword(e.target.value)} required />
        </label>
        <label>
          New password
          <input type="password" value={newPassword} onChange={(e) => setNewPassword(e.target.value)} required minLength={6} />
        </label>
        <label>
          Confirm new password
          <input type="password" value={confirmPassword} onChange={(e) => setConfirmPassword(e.target.value)} required minLength={6} />
        </label>
        {error && <div className="page-error">{error}</div>}
        {success && <div className="page-sub">Password changed.</div>}
        <button type="submit" disabled={saving}>
          {saving ? 'Saving...' : 'Change password'}
        </button>
      </form>
    </div>
  )
}

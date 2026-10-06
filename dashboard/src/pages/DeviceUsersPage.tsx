import { useEffect, useState, type FormEvent } from 'react'
import { Link, useLocation, useParams } from 'react-router-dom'
import { createUsers, enumUsers } from '../api/device'
import { useApiCall } from '../api/useApiCall'
import { ResponseState, type PanelUser } from '../api/types'
import './DataPage.css'
import './DeviceUsersPage.css'

interface LocationState {
  description?: string
  identifier?: string
}

export function DeviceUsersPage() {
  const { deviceId: deviceIdParam } = useParams<{ deviceId: string }>()
  const deviceId = Number(deviceIdParam)
  const { state } = useLocation() as { state: LocationState | null }
  const call = useApiCall()

  const [users, setUsers] = useState<PanelUser[] | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [userNumber, setUserNumber] = useState('')
  const [userName, setUserName] = useState('')
  const [saving, setSaving] = useState(false)
  const [saveError, setSaveError] = useState<string | null>(null)

  function load() {
    call(() => enumUsers(deviceId))
      .then((res) => {
        if (res.State === ResponseState.NOT_FOUND) {
          setError('Device not found.')
          return
        }
        setUsers(res.Users ?? [])
      })
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
    <div>
      <Link className="row-link" to={`/devices/${deviceId}`}>&larr; Back to device</Link>
      <h1 style={{ marginTop: 12 }}>
        {state?.description ? `Users on ${state.description}` : `Users on device #${deviceId}`}
      </h1>
      <p className="page-sub">
        {state?.identifier ? <>Panel identifier: <code>{state.identifier}</code>. </> : null}
        PIN-holder labels for this panel -- who each user number belongs to.
      </p>

      {error && <div className="page-error">{error}</div>}

      {!error && !users && <div className="page-loading">Loading...</div>}

      {users && (
        <>
          <div className="table-wrap">
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
            <h2>Add / edit a user</h2>
            <label>
              User number
              <input
                type="number"
                min={0}
                value={userNumber}
                onChange={(e) => setUserNumber(e.target.value)}
                required
              />
            </label>
            <label>
              Name
              <input
                type="text"
                value={userName}
                onChange={(e) => setUserName(e.target.value)}
                required
              />
            </label>
            {saveError && <div className="page-error">{saveError}</div>}
            <button type="submit" disabled={saving}>
              {saving ? 'Saving...' : 'Save'}
            </button>
          </form>
        </>
      )}
    </div>
  )
}

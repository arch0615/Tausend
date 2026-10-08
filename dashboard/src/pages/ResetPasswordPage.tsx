import { useState, type FormEvent } from 'react'
import { useSearchParams, Link } from 'react-router-dom'
import { apiPost } from '../api/client'
import { ResponseState, type BaseResponse } from '../api/types'
import './LoginPage.css'

// Public page (no auth) -- reached by clicking the link in the password-recovery email that
// AccountService/RecoverPassword sends (see backend-core's EmailBusiness/AccountBusiness). The
// token in the URL is single-use and time-limited; ResetPassword just validates it server-side,
// there's nothing to check client-side beyond "is it present".
export function ResetPasswordPage() {
  const [params] = useSearchParams()
  const token = params.get('token') ?? ''
  const [password, setPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  // Client issue #12: this page had no way to see what was typed, on a screen where you must
  // type the same new password twice and cannot paste-check it anywhere. One toggle drives
  // both fields, so they are always shown or hidden together.
  const [showPassword, setShowPassword] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)
  const [done, setDone] = useState(false)

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    if (!token) {
      setError('Este enlace no es válido. Solicitá uno nuevo desde la app.')
      return
    }
    if (password.length < 8) {
      setError('La contraseña debe tener al menos 8 caracteres.')
      return
    }
    if (password !== confirmPassword) {
      setError('Las contraseñas no coinciden.')
      return
    }
    setError(null)
    setSubmitting(true)
    try {
      const res = await apiPost<BaseResponse>('AccountService/ResetPassword', {
        ResetToken: token,
        NewPassword: password,
      })
      if (res.State === ResponseState.OK) {
        setDone(true)
      } else {
        setError(res.Message || 'No se pudo restablecer la contraseña.')
      }
    } catch {
      setError('No se pudo conectar con el servidor. Intentá de nuevo.')
    } finally {
      setSubmitting(false)
    }
  }

  if (done) {
    return (
      <div className="login-page">
        <div className="login-card">
          <h1>Contraseña actualizada</h1>
          <p className="subtitle">Ya podés iniciar sesión con tu nueva contraseña, tanto en la app como acá.</p>
          <Link to="/login">Ir a iniciar sesión</Link>
        </div>
      </div>
    )
  }

  return (
    <div className="login-page">
      <form className="login-card" onSubmit={handleSubmit}>
        <h1>Restablecer contraseña</h1>
        <p className="subtitle">Elegí una nueva contraseña para tu cuenta.</p>

        {!token && (
          <div className="error">Este enlace no es válido o está incompleto. Solicitá uno nuevo desde la app.</div>
        )}

        <label>
          Nueva contraseña
          <input
            type={showPassword ? 'text' : 'password'}
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            required
            autoFocus
          />
        </label>

        <label>
          Confirmar contraseña
          <input
            type={showPassword ? 'text' : 'password'}
            value={confirmPassword}
            onChange={(e) => setConfirmPassword(e.target.value)}
            required
          />
        </label>

        <label className="show-password">
          <input type="checkbox" checked={showPassword} onChange={(e) => setShowPassword(e.target.checked)} />
          Mostrar contraseña
        </label>

        {error && <div className="error">{error}</div>}

        <button type="submit" disabled={submitting || !token}>
          {submitting ? 'Guardando...' : 'Guardar contraseña'}
        </button>
      </form>
    </div>
  )
}

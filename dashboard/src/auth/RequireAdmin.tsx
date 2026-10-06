import { Navigate, Outlet } from 'react-router-dom'
import { useAuth } from './AuthContext'

// Client-side gate only, for UX (don't show the shell to a non-admin).
// The real authorization boundary is server-side -- every AdminService call
// re-checks the token's Role on the backend regardless of what this renders.
export function RequireAdmin() {
  const { session, isAdmin, ready } = useAuth()

  // Brief window on first load (a stored session exists but hasn't been handed to apiPost's
  // token interceptor yet) -- wait rather than flash a redirect to /login before it's primed.
  if (!ready) {
    return null
  }
  if (!session) {
    return <Navigate to="/login" replace />
  }
  if (!isAdmin) {
    return <Navigate to="/login" replace />
  }
  return <Outlet />
}

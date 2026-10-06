import { useCallback } from 'react'
import { useNavigate } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'
import { ResponseState, type BaseResponse } from './types'

// Every data page used to repeat the exact same "await the call, check State === UNAUTHORIZED,
// logout() + navigate to /login if so" block by hand (see ARCHITECTURE.txt's gap #6). This hook
// centralizes it once -- and goes further than the old per-page checks did: on UNAUTHORIZED it
// tries a silent refresh via the stored RefreshToken first (see AuthContext.refresh()), and only
// forces a full re-login if that also fails. Takes a thunk (not an already-started promise) so
// the call can actually be retried with the refreshed token, not just failed more gracefully.
export function useApiCall() {
  const { logout, refresh } = useAuth()
  const navigate = useNavigate()

  return useCallback(
    async <T extends BaseResponse>(thunk: () => Promise<T>): Promise<T> => {
      const res = await thunk()
      if (res.State !== ResponseState.UNAUTHORIZED) return res

      const refreshed = await refresh()
      if (!refreshed) {
        logout()
        navigate('/login', { replace: true })
        return res
      }
      return thunk()
    },
    [logout, refresh, navigate],
  )
}

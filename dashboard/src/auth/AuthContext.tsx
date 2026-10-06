import { createContext, useContext, useState, useCallback, useEffect, type ReactNode } from 'react'
import { login as apiLogin, refreshAccessToken } from '../api/auth'
import { setAccessToken } from '../api/client'
import { AccountRole, type Account } from '../api/types'

interface Session {
  account: Account
}

interface AuthContextValue {
  session: Session | null
  isAdmin: boolean
  ready: boolean
  login: (email: string, password: string) => Promise<{ ok: true } | { ok: false; message: string }>
  logout: () => void
  // Tries to silently refresh the session using the stored RefreshToken. Returns whether it
  // worked -- callers (see useApiCall) fall back to a full logout only if this fails too.
  refresh: () => Promise<boolean>
}

const AuthContext = createContext<AuthContextValue | null>(null)

const STORAGE_KEY = 'tausend-admin-session'

function loadStoredSession(): Session | null {
  const raw = localStorage.getItem(STORAGE_KEY)
  if (!raw) return null
  try {
    return JSON.parse(raw) as Session
  } catch {
    return null
  }
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [session, setSessionState] = useState<Session | null>(loadStoredSession)
  // False only for the brief window on first load where a stored session exists but hasn't been
  // handed to apiPost's interceptor yet -- prevents a page's first fetch from firing with no
  // token attached. True immediately when there's no stored session at all (nothing to wait for).
  const [ready, setReady] = useState(() => !loadStoredSession())

  function setSession(next: Session | null) {
    setAccessToken(next?.account.AccessToken ?? null)
    setSessionState(next)
    if (next) {
      localStorage.setItem(STORAGE_KEY, JSON.stringify(next))
    } else {
      localStorage.removeItem(STORAGE_KEY)
    }
  }

  // Runs once on mount -- primes apiPost's token interceptor from whatever was in localStorage
  // before any page gets a chance to fetch.
  useEffect(() => {
    if (session) setAccessToken(session.account.AccessToken)
    setReady(true)
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  const login = useCallback(async (email: string, password: string) => {
    const response = await apiLogin(email, password)
    if (!response.Account) {
      return { ok: false as const, message: response.Message || 'Login failed' }
    }
    if (response.Account.Role !== AccountRole.Admin) {
      return { ok: false as const, message: 'This account does not have admin access.' }
    }
    setSession({ account: response.Account })
    return { ok: true as const }
  }, [])

  const logout = useCallback(() => {
    setSession(null)
  }, [])

  const refresh = useCallback(async () => {
    if (!session?.account.RefreshToken) return false
    try {
      const response = await refreshAccessToken(session.account.RefreshToken)
      if (!response.Account || response.Account.Role !== AccountRole.Admin) return false
      setSession({ account: response.Account })
      return true
    } catch {
      return false
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [session])

  const value: AuthContextValue = {
    session,
    isAdmin: session?.account.Role === AccountRole.Admin,
    ready,
    login,
    logout,
    refresh,
  }

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth must be used within AuthProvider')
  return ctx
}

import { createContext, useContext, useEffect, useState, type ReactNode } from 'react';
import { login as apiLogin, logout as apiLogout, refreshAccessToken } from '../api/auth';
import { setAccessToken } from '../api/client';
import { ResponseState, type Account } from '../api/types';
import { clearTokens, loadTokens, saveTokens } from './tokenStorage';
import {
  getCurrentDeviceToken,
  initPushNotifications,
  subscribeToForegroundMessages,
  subscribeToTokenRefresh,
} from '../notifications/messaging';

interface Session {
  account: Account;
}

interface AuthContextValue {
  session: Session | null;
  isLoading: boolean;
  login: (
    email: string,
    password: string,
  ) => Promise<{ ok: true; pinChanged: boolean; removedDevices: Account['RemovedDevices'] } | { ok: false; message: string }>;
  logout: () => Promise<void>;
  // Re-fetches the account (via the stored refresh token) so screens that change server-side
  // state outside a login -- e.g. pairing a new panel -- can pull the update into session.account
  // without waiting for the next app launch. No-ops quietly on failure; callers that need to know
  // whether it worked should re-check session afterward.
  refreshSession: () => Promise<void>;
}

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [session, setSession] = useState<Session | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  // Independent of session state -- if a refresh fires while logged out, registerDeviceToken's
  // own apiPost call just goes out without an AccessToken and fails harmlessly server-side.
  useEffect(() => subscribeToTokenRefresh(), []);

  // FCM never auto-displays a notification while the app is in the foreground -- without this,
  // a message arriving while the app was open was silently dropped (see subscribeToForegroundMessages).
  useEffect(() => subscribeToForegroundMessages(), []);

  useEffect(() => {
    let cancelled = false;
    (async () => {
      const stored = await loadTokens();
      if (!stored) {
        setIsLoading(false);
        return;
      }
      try {
        const res = await refreshAccessToken(stored.refreshToken);
        if (cancelled) return;
        if (res.State === ResponseState.OK && res.Account) {
          setAccessToken(res.Account.AccessToken);
          await saveTokens({ accessToken: res.Account.AccessToken, refreshToken: res.Account.RefreshToken });
          setSession({ account: res.Account });
          initPushNotifications();
        } else {
          // Refresh token is dead (expired/revoked) -- the stored session can't be
          // restored, so drop it rather than keep retrying it on every launch.
          await clearTokens();
        }
      } catch {
        // Network/server unreachable at launch -- leave the stored tokens alone (they
        // may still be valid) and just fall back to the sign-in screen for this session.
      } finally {
        if (!cancelled) setIsLoading(false);
      }
    })();
    return () => {
      cancelled = true;
    };
  }, []);

  async function refreshSession() {
    const stored = await loadTokens();
    if (!stored) return;
    try {
      const res = await refreshAccessToken(stored.refreshToken);
      if (res.State === ResponseState.OK && res.Account) {
        setAccessToken(res.Account.AccessToken);
        await saveTokens({ accessToken: res.Account.AccessToken, refreshToken: res.Account.RefreshToken });
        setSession({ account: res.Account });
      }
    } catch {
      // Best-effort -- leave the current session as-is if this fails.
    }
  }

  async function login(email: string, password: string) {
    const res = await apiLogin(email, password);
    if (!res.Account) {
      return { ok: false as const, message: res.Message || 'Login failed' };
    }
    setAccessToken(res.Account.AccessToken);
    await saveTokens({ accessToken: res.Account.AccessToken, refreshToken: res.Account.RefreshToken });
    setSession({ account: res.Account });
    initPushNotifications();
    return { ok: true as const, pinChanged: res.PinChanged, removedDevices: res.Account.RemovedDevices ?? [] };
  }

  async function logout() {
    // Captured before clearing -- apiPost's 20s request timeout must never hold up the visible
    // logout, so the backend call below fires in the background instead of being awaited here.
    const accessToken = session?.account.AccessToken ?? null;
    setAccessToken(null);
    await clearTokens();
    setSession(null);

    if (accessToken) {
      getCurrentDeviceToken()
        .then((deviceToken) => (deviceToken ? apiLogout(accessToken, deviceToken) : undefined))
        .catch(() => {
          // Best-effort -- the token stays registered server-side until the next successful
          // logout or it's moved to another account, but the user is signed out either way.
        });
    }
  }

  const value: AuthContextValue = { session, isLoading, login, logout, refreshSession };
  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be used within AuthProvider');
  return ctx;
}

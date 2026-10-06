import { apiPost } from './client'
import type { AccountLoginResponse, BaseResponse } from './types'

// Deliberately doesn't go through the token-auto-injecting apiPost path for the token itself
// (there isn't one yet at login time) -- Email/Password only, matching the mobile app's login().
export function login(email: string, password: string): Promise<AccountLoginResponse> {
  return apiPost<AccountLoginResponse>('AccountService/Login', { Email: email, Password: password })
}

// Silently refreshes the session using the long-lived RefreshToken instead of forcing the admin
// to fully re-enter credentials every time the (short-lived) AccessToken expires.
export function refreshAccessToken(refreshToken: string): Promise<AccountLoginResponse> {
  return apiPost<AccountLoginResponse>('AccountService/RefreshAccessToken', { RefreshToken: refreshToken })
}

// Changes the CURRENTLY SIGNED-IN admin's own password -- not an admin-on-someone-else action.
export function updateOwnPassword(oldPassword: string, newPassword: string): Promise<BaseResponse> {
  return apiPost<BaseResponse>('AccountService/UpdateAccount', { OldPassword: oldPassword, NewPassword: newPassword })
}

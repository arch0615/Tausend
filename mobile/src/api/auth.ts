import { apiPost } from './client';
import type { AccountLoginResponse, BaseResponse } from './types';

export function login(email: string, password: string): Promise<AccountLoginResponse> {
  return apiPost<AccountLoginResponse>('AccountService/Login', { Email: email, Password: password });
}

export function createAccount(
  email: string,
  password: string,
  firstName: string,
  lastName: string,
): Promise<BaseResponse> {
  return apiPost<BaseResponse>('AccountService/CreateAccount', {
    Email: email,
    Password: password,
    FirstName: firstName,
    LastName: lastName,
  });
}

export function refreshAccessToken(refreshToken: string): Promise<AccountLoginResponse> {
  return apiPost<AccountLoginResponse>('AccountService/RefreshAccessToken', { RefreshToken: refreshToken });
}

// Always reports success server-side regardless of whether the email is registered,
// so the response can't be used to enumerate accounts -- callers should show the same
// "check your email" message unconditionally rather than branching on the result.
export function recoverPassword(email: string): Promise<BaseResponse> {
  return apiPost<BaseResponse>('AccountService/RecoverPassword', { Email: email });
}

// Requires an active session -- apiPost injects the current AccessToken automatically.
export function updateAccountPassword(oldPassword: string, newPassword: string): Promise<BaseResponse> {
  return apiPost<BaseResponse>('AccountService/UpdateAccount', {
    OldPassword: oldPassword,
    NewPassword: newPassword,
  });
}

// Removes this device's push token from the account server-side (see Logout.sql) so alarm
// notifications stop arriving once the user has signed out. Takes accessToken explicitly (rather
// than relying on apiPost's auto-injected one) because AuthContext fires this after already
// clearing the module-level token, so the visible logout isn't held up by this network call.
export function logout(accessToken: string, deviceToken: string): Promise<BaseResponse> {
  return apiPost<BaseResponse>('AccountService/Logout', { AccessToken: accessToken, DeviceToken: deviceToken });
}

// Soft-deletes the account server-side (Accounts.Enabled=0, see DeleteAccount.sql) and revokes
// its access tokens -- requires an active session -- apiPost injects the current AccessToken
// automatically.
export function deleteAccount(): Promise<BaseResponse> {
  return apiPost<BaseResponse>('AccountService/DeleteAccount', {});
}

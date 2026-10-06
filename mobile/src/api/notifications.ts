import { apiPost } from './client';
import type { BaseResponse } from './types';

// Idempotent upsert server-side (CreateDeviceToken.sql moves the token off any other account it
// was previously registered to, or updates OS if it already belongs to this one) -- safe to call
// on every login and again whenever the push library reports a refreshed token. OS casing doesn't
// matter to the backend (PhoneOSHelper.ToPhoneType uppercases before comparing), but it must
// spell "android"/"ios" or notification building throws server-side for that device.
export function registerDeviceToken(token: string, os: 'Android' | 'iOS'): Promise<BaseResponse> {
  return apiPost<BaseResponse>('AccountService/CreateAccountDeviceToken', { OS: os, Token: token });
}

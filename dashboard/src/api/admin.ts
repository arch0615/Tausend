import { apiPost } from './client'
import type {
  AccountRole,
  ListOfAdminAccountsResponse,
  ListOfAdminDevicesResponse,
  ListOfAccountDeviceLinksResponse,
  ListOfAuditLogResponse,
  ListOfEventsResponse,
  BaseResponse,
} from './types'

export function enumAllAccounts(): Promise<ListOfAdminAccountsResponse> {
  return apiPost<ListOfAdminAccountsResponse>('AdminService/EnumAllAccounts')
}

export function enumAllDevices(): Promise<ListOfAdminDevicesResponse> {
  return apiPost<ListOfAdminDevicesResponse>('AdminService/EnumAllDevices')
}

// Every AccountId<->DeviceId link in the fleet, one row per pair -- correlated client-side
// against enumAllAccounts()/enumAllDevices() to answer "which devices does this account have"
// and "which accounts have this device" (see AccountDetailPage/DeviceDetailPage).
export function enumAccountDeviceLinks(): Promise<ListOfAccountDeviceLinksResponse> {
  return apiPost<ListOfAccountDeviceLinksResponse>('AdminService/EnumAccountDeviceLinks')
}

export function setAccountRole(accountId: number, role: AccountRole): Promise<BaseResponse> {
  return apiPost<BaseResponse>('AdminService/SetAccountRole', { AccountId: accountId, Role: role })
}

// Also revokes the account's current sessions when disabling -- see AdminBusiness.SetAccountEnabled.
export function setAccountEnabled(accountId: number, enabled: boolean): Promise<BaseResponse> {
  return apiPost<BaseResponse>('AdminService/SetAccountEnabled', { AccountId: accountId, Enabled: enabled })
}

export function revokeAccountSessions(accountId: number): Promise<BaseResponse> {
  return apiPost<BaseResponse>('AdminService/RevokeAccountSessions', { AccountId: accountId })
}

export function unlinkAccountDevice(accountId: number, deviceId: number): Promise<BaseResponse> {
  return apiPost<BaseResponse>('AdminService/UnlinkAccountDevice', { AccountId: accountId, DeviceId: deviceId })
}

// Admin-only, unscoped device actions -- bypass the normal ownership check because the caller's
// admin status is verified server-side instead. See AdminBusiness.BlockDevice/ResetDevice/DisassociateDevice.
export function blockDeviceAsAdmin(deviceId: number): Promise<BaseResponse> {
  return apiPost<BaseResponse>('AdminService/BlockDevice', { DeviceId: deviceId })
}

export function resetDeviceAsAdmin(deviceId: number): Promise<BaseResponse> {
  return apiPost<BaseResponse>('AdminService/ResetDevice', { DeviceId: deviceId })
}

export function disassociateDeviceAsAdmin(deviceId: number): Promise<BaseResponse> {
  return apiPost<BaseResponse>('AdminService/DisassociateDevice', { DeviceId: deviceId })
}

export function enumAuditLog(): Promise<ListOfAuditLogResponse> {
  return apiPost<ListOfAuditLogResponse>('AdminService/EnumAuditLog')
}

// Fleet-wide event history (not scoped to one device) -- last 200, newest first.
export function enumAllEvents(): Promise<ListOfEventsResponse> {
  return apiPost<ListOfEventsResponse>('AdminService/EnumAllEvents')
}

// Plain access toggle -- unlike blockDeviceAsAdmin/resetDeviceAsAdmin, doesn't touch account
// links or tokens either way. Re-enabling ("Unblock access") doesn't restore anything a prior
// Reset PIN may have already unlinked.
export function setDeviceEnabled(deviceId: number, enabled: boolean): Promise<BaseResponse> {
  return apiPost<BaseResponse>('AdminService/SetDeviceEnabled', { DeviceId: deviceId, Enabled: enabled })
}

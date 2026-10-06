import { apiPost } from './client'
import type { BaseResponse, ListOfEventsResponse, ListOfUserResponse, PanelUser } from './types'

// GetDeviceByID/EnumUsers/CreateUsers all now accept either the device owner's own token
// or an Admin's -- see backend/DAY5_SUMMARY.md and the AuthorizeDeviceAccess helper in
// DeviceService.svc.cs / DeviceController.cs. The dashboard always calls these as an Admin
// managing someone else's panel, not as the device's owner.

export function enumUsers(deviceId: number): Promise<ListOfUserResponse> {
  return apiPost<ListOfUserResponse>('DeviceService/EnumUsers', { DeviceId: deviceId })
}

// The backend upserts by (DeviceId, UserNumber) -- safe to call with just the one entry
// being added/edited, no need to resend the whole list.
export function createUsers(deviceId: number, users: Pick<PanelUser, 'UserNumber' | 'UserName'>[]): Promise<BaseResponse> {
  return apiPost<BaseResponse>('DeviceService/CreateUsers', {
    DeviceId: deviceId,
    Users: users.map((u) => ({ UserNumber: u.UserNumber, UserName: u.UserName, DeviceId: deviceId })),
  })
}

// Admin-scoped equivalent of the mobile app's own event history -- works against any device now
// that NotificationController.EnumEvents has the same owner-or-admin check as everything else.
export function enumEvents(deviceId: number): Promise<ListOfEventsResponse> {
  return apiPost<ListOfEventsResponse>('NotificationService/EnumEvents', { DeviceId: deviceId })
}

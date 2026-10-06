import { apiPost } from './client';
import type { NewCreatedDeviceResponse, BaseResponse, DisassociateCentralResponse, TimeResponse } from './types';

// AccessToken is injected automatically by apiPost (see client.ts) -- every function here only
// takes the fields specific to that call.

// Live-validates the PIN against the panel itself (through the relay), so this can take a few
// seconds and can fail with "offline" as well as "wrong PIN" -- see DeviceBusiness.ValidatePinOnDevice.
export function createDevice(
  description: string,
  identifier: string,
  pin: string,
  email?: string,
): Promise<NewCreatedDeviceResponse> {
  return apiPost<NewCreatedDeviceResponse>('DeviceService/CreateDevice', {
    Description: description,
    Identifier: identifier,
    Pin: pin,
    Email: email,
  });
}

export function updateDevice(
  deviceId: number,
  description: string,
  identifier: string,
  pin: string,
): Promise<BaseResponse> {
  return apiPost<BaseResponse>('DeviceService/UpdateDevice', {
    DeviceId: deviceId,
    Description: description,
    Identifier: identifier,
    Pin: pin,
  });
}

// SMS-connected panels: no live PIN-on-panel validation (there's no relay path to one), this just
// registers the panel's SIM phone number + PIN so later commands can be texted to it.
export function createDeviceSms(
  description: string,
  identifier: string,
  devicePin: string,
  simPin: string,
  phoneNumber: string,
  deviceType: string,
): Promise<BaseResponse> {
  return apiPost<BaseResponse>('DeviceService/CreateDeviceSMS', {
    Description: description,
    Identifier: identifier,
    DevicePin: devicePin,
    SimPin: simPin,
    PhoneNumber: phoneNumber,
    DeviceType: deviceType,
  });
}

// DeviceTime/ServerTime come back as plain ISO-8601 strings (backend-core has no custom DateTime
// converter) -- new Date(iso) directly, no old-app-style "/Date(...)/" parsing or manual UTC-3
// offset needed, that offset is already baked in server-side (CommandBusiness.GetCurrentDateTime).
export function getTime(deviceId: number): Promise<TimeResponse> {
  return apiPost<TimeResponse>('DeviceService/GetTime', { DeviceId: deviceId });
}

// Sets the panel's clock to the server's current time (not the phone's) -- same GetTime response
// shape, DeviceTime reflects what the panel echoed back after the sync command.
export function syncTime(deviceId: number): Promise<TimeResponse> {
  return apiPost<TimeResponse>('DeviceService/SyncTime', { DeviceId: deviceId });
}

export function updateDeviceSms(
  deviceId: number,
  description: string,
  identifier: string,
  devicePin: string,
  simPin: string,
  phoneNumber: string,
  deviceType: string,
): Promise<BaseResponse> {
  return apiPost<BaseResponse>('DeviceService/UpdateDeviceSMS', {
    DeviceId: deviceId,
    Description: description,
    Identifier: identifier,
    DevicePin: devicePin,
    SimPin: simPin,
    PhoneNumber: phoneNumber,
    DeviceType: deviceType,
  });
}

// Removes only the caller's own link to this IP panel -- the panel keeps working for any other
// co-owner, and its Devices row is untouched. Keyed by Identifier (Mac), not DeviceId. Unlike
// most calls here, the account's own session stays valid afterward -- call refreshSession(), not
// logout(), so session.account.Devices drops the panel without ending the current login.
export function disassociateCentral(identifier: string): Promise<DisassociateCentralResponse> {
  return apiPost<DisassociateCentralResponse>('DeviceService/DissasociateCentral', { Identifier: identifier });
}

// Hard delete -- SMS devices aren't shared (no AccountDevicePins-style join table), so there's no
// "just my link" concept here, unlike disassociateCentral above.
export function deleteDeviceSms(deviceId: number): Promise<BaseResponse> {
  return apiPost<BaseResponse>('DeviceService/DeleteDeviceSMS', { DeviceId: deviceId });
}

// Block: orphans the PIN mapping (app control revoked) but leaves the Devices row enabled -- the
// physical panel keeps working, only app access is dead. Reset: also disables the Devices row and
// deletes the pairing outright (re-pairing from scratch required). Both force-logout every
// account linked to the panel server-side, including the caller. BlockPIN used to return a bare
// string with no State/Code -- fixed server-side (both backends) to return the same
// BaseResponse-shaped object as everything else, since nothing was calling it yet.
export function blockPin(identifier: string, action: 'Block' | 'Reset'): Promise<BaseResponse> {
  return apiPost<BaseResponse>('DeviceService/BlockPIN', { Identifier: identifier, Action: action });
}

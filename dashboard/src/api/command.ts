import { apiPost } from './client'
import type {
  BaseResponse,
  BatteryStateResponse,
  CommandResponse,
  FailStatusResponse,
  ListOfProgramControlResponse,
  ListOfZonesResponse,
  TimeResponse,
} from './types'

// Live panel diagnostics, called with the signed-in Admin's own token against ANY device in the
// fleet -- not just one the admin's own account happens to own. This only works because
// CommandController's device-ownership check has an Admin bypass (AuthorizeDeviceAccess,
// backend-core), and CommandBusiness.CreateBody resolves the device unscoped by DeviceId rather
// than from the caller's own account.Devices -- both already true as of the redesign that added
// this file. Support staff can use this to see what's actually wrong with a customer's panel
// without needing the customer's own phone.

export function getGeneralStatus(deviceId: number): Promise<CommandResponse> {
  return apiPost<CommandResponse>('CommandService/GetGeneralStatus', { DeviceId: deviceId })
}

export function getBatteryStatus(deviceId: number): Promise<BatteryStateResponse> {
  return apiPost<BatteryStateResponse>('CommandService/GetBatteryStatus', { DeviceId: deviceId, Command: '' })
}

export function getFailStatus(deviceId: number): Promise<FailStatusResponse> {
  return apiPost<FailStatusResponse>('CommandService/GetFailStatus', { DeviceId: deviceId })
}

export function getZones(deviceId: number): Promise<ListOfZonesResponse> {
  return apiPost<ListOfZonesResponse>('DeviceService/GetZones', { DeviceId: deviceId })
}

// Upserts by (DeviceId, ZoneNumber) -- safe to send just the one zone being renamed.
export function renameZone(deviceId: number, zoneNumber: number, name: string): Promise<BaseResponse> {
  return apiPost<BaseResponse>('DeviceService/CreateZones', {
    DeviceId: deviceId,
    Zones: [{ DeviceId: deviceId, ZoneNumber: zoneNumber, Name: name }],
  })
}

// Zones is the FULL desired set of excluded/bypassed zone numbers, not a single toggle -- the
// caller must send every zone number that should stay excluded, including the ones already
// excluded before this call.
export function setExclusions(deviceId: number, excludedZoneNumbers: number[]): Promise<CommandResponse> {
  return apiPost<CommandResponse>('CommandService/Exclusion', { DeviceId: deviceId, Zones: excludedZoneNumbers })
}

export function getProgramControls(deviceId: number): Promise<ListOfProgramControlResponse> {
  return apiPost<ListOfProgramControlResponse>('DeviceService/GetProgramControls', { DeviceId: deviceId })
}

export function toggleProgramControl(deviceId: number, zone: number, state: boolean): Promise<ListOfProgramControlResponse> {
  return apiPost<ListOfProgramControlResponse>('CommandService/ProgramControl', { DeviceId: deviceId, Zone: zone, State: state })
}

export function getTime(deviceId: number): Promise<TimeResponse> {
  return apiPost<TimeResponse>('DeviceService/GetTime', { DeviceId: deviceId })
}

export function syncTime(deviceId: number): Promise<TimeResponse> {
  return apiPost<TimeResponse>('DeviceService/SyncTime', { DeviceId: deviceId })
}

// Raw low-level panel command -- Admin/Installer only (server-side gated), every call recorded
// in the audit log regardless of the panel's response. Not for routine use.
export function sendInstallerCommand(deviceId: number, command: string): Promise<CommandResponse> {
  return apiPost<CommandResponse>('CommandService/SendInstallerCommand', { DeviceId: deviceId, Command: command })
}

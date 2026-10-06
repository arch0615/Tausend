import { apiPost } from './client';
import type { BaseResponse, CommandResponse, ListOfExclusionsResponse, ListOfZonesResponse } from './types';

export function getZones(deviceId: number): Promise<ListOfZonesResponse> {
  return apiPost<ListOfZonesResponse>('DeviceService/GetZones', { DeviceId: deviceId });
}

// Batch save -- callers send only the zones that actually changed, each as {ZoneNumber, Name}.
export function saveZoneNames(
  deviceId: number,
  zones: { ZoneNumber: number; Name: string }[],
): Promise<BaseResponse> {
  return apiPost<BaseResponse>('DeviceService/CreateZones', { DeviceId: deviceId, Zones: zones });
}

export function getExclusions(deviceId: number): Promise<ListOfExclusionsResponse> {
  return apiPost<ListOfExclusionsResponse>('DeviceService/GetExclusions', { DeviceId: deviceId });
}

// Live bypass command (through the relay, CommandService/Exclusion -> BYP) -- sends the *entire*
// current bypass list, not an incremental toggle. zoneNumbers are 1-indexed zone numbers.
export function sendExclusions(deviceId: number, zoneNumbers: number[]): Promise<CommandResponse> {
  return apiPost<CommandResponse>('CommandService/Exclusion', { DeviceId: deviceId, Zones: zoneNumbers });
}

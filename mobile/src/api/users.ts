import { apiPost } from './client';
import type { BaseResponse, ListOfUserResponse } from './types';

// Returns only PIN-holder labels that already exist for this device -- no synthesized 1..N fill
// like GetZones/GetProgramControls, since user numbers are freely chosen, not a fixed panel count.
export function getUsers(deviceId: number): Promise<ListOfUserResponse> {
  return apiPost<ListOfUserResponse>('DeviceService/EnumUsers', { DeviceId: deviceId });
}

// Upsert -- same endpoint adds new labels and edits existing ones (matched by UserNumber). There
// is no delete endpoint; removing a label means saving it with an empty UserName.
export function saveUsers(
  deviceId: number,
  users: { UserNumber: number; UserName: string }[],
): Promise<BaseResponse> {
  return apiPost<BaseResponse>('DeviceService/CreateUsers', { DeviceId: deviceId, Users: users });
}

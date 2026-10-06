import { apiPost } from './client';
import type { ListOfZonesResponse } from './types';

// The panel's own memory of which zones triggered since the last arm/reset -- server-side
// already filters to just the zones with memory (CommandBusiness.GetMemoryStatus), so the
// response reuses the same Zone/ListOfZonesResponse shape as GetZones, just pre-filtered.
export function getMemory(deviceId: number): Promise<ListOfZonesResponse> {
  return apiPost<ListOfZonesResponse>('DeviceService/GetMemory', { DeviceId: deviceId });
}

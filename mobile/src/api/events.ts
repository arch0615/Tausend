import { apiPost } from './client';
import type { ListOfEventsResponse } from './types';

// Pure DB read (top 200 events, newest first) -- never talks to the panel, unlike Zones/PGM's
// live-polled reads, so there's no CENTRAL_UNRESPONSIVE case to handle here.
export function getEvents(deviceId: number): Promise<ListOfEventsResponse> {
  return apiPost<ListOfEventsResponse>('NotificationService/EnumEvents', { DeviceId: deviceId });
}

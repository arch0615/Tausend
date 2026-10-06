import { apiPost } from './client';
import type {
  BaseResponse,
  CreatedScheduledPgmActionResponse,
  ListOfScheduledPgmActionsResponse,
} from './types';

export function createSchedule(
  deviceId: number,
  programControlNumber: number,
  timeOfDay: string,
  daysOfWeekMask: number,
  desiredState: boolean,
): Promise<CreatedScheduledPgmActionResponse> {
  return apiPost<CreatedScheduledPgmActionResponse>('DeviceService/CreateScheduledPgmAction', {
    DeviceId: deviceId,
    ProgramControlNumber: programControlNumber,
    TimeOfDay: timeOfDay,
    DaysOfWeekMask: daysOfWeekMask,
    DesiredState: desiredState,
  });
}

export function enumSchedules(deviceId: number): Promise<ListOfScheduledPgmActionsResponse> {
  return apiPost<ListOfScheduledPgmActionsResponse>('DeviceService/EnumScheduledPgmActions', { DeviceId: deviceId });
}

export function deleteSchedule(deviceId: number, scheduledPgmActionId: number): Promise<BaseResponse> {
  return apiPost<BaseResponse>('DeviceService/DeleteScheduledPgmAction', {
    DeviceId: deviceId,
    ScheduledPgmActionId: scheduledPgmActionId,
  });
}

export function setScheduleEnabled(
  deviceId: number,
  scheduledPgmActionId: number,
  enabled: boolean,
): Promise<BaseResponse> {
  return apiPost<BaseResponse>('DeviceService/SetScheduledPgmActionEnabled', {
    DeviceId: deviceId,
    ScheduledPgmActionId: scheduledPgmActionId,
    Enabled: enabled,
  });
}

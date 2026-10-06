import { apiPost } from './client';
import type { BaseResponse, ListOfProgramControlResponse } from './types';

// Always returns exactly 8 outputs, each live-polled from the panel -- slower than
// GetZones/GetExclusions (8 sequential relay round-trips server-side).
export function getProgramControls(deviceId: number): Promise<ListOfProgramControlResponse> {
  return apiPost<ListOfProgramControlResponse>('DeviceService/GetProgramControls', { DeviceId: deviceId });
}

// Renames only -- does not touch the panel's physical output state. Callers send only the
// outputs that actually changed, each as {ProgramControlNumber, Name}.
export function saveProgramControlNames(
  deviceId: number,
  programControls: { ProgramControlNumber: number; Name: string }[],
): Promise<BaseResponse> {
  return apiPost<BaseResponse>('DeviceService/CreateProgramControls', {
    DeviceId: deviceId,
    ProgramControls: programControls,
  });
}

// Live toggle, not a momentary pulse -- State persists on the panel until toggled again. The
// backend's PGMRequest names the target output "Zone", not "ProgramControlNumber"; kept as-is
// here since that's the literal wire field the server expects (CommandController.ProgramControl).
// Response contains just the one toggled output, re-read live from the panel after the command.
export function toggleProgramControl(
  deviceId: number,
  pgm: number,
  state: boolean,
): Promise<ListOfProgramControlResponse> {
  return apiPost<ListOfProgramControlResponse>('CommandService/ProgramControl', {
    DeviceId: deviceId,
    Zone: pgm,
    State: state,
  });
}

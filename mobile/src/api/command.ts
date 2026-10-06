import { apiPost } from './client';
import type { BatteryStateResponse, CommandResponse, FailStatusResponse } from './types';

// All of these take only DeviceId -- AccessToken is injected by apiPost, and the backend looks
// up the panel's own identifier/PIN server-side from the caller's account (CommandBusiness.
// CreateBody), so a caller can never operate a device that isn't their own. Routes mirror the
// old WCF UriTemplates minus the "/Services" + ".svc" prefix -- see backend-core's
// Controllers/CommandController.cs.

export function armAlarm(deviceId: number): Promise<CommandResponse> {
  return apiPost<CommandResponse>('CommandService/ArmAlarm', { DeviceId: deviceId });
}

export function dayArmAlarm(deviceId: number): Promise<CommandResponse> {
  return apiPost<CommandResponse>('CommandService/DayArmAlarm', { DeviceId: deviceId });
}

export function nightArmAlarm(deviceId: number): Promise<CommandResponse> {
  return apiPost<CommandResponse>('CommandService/NightArmAlarm', { DeviceId: deviceId });
}

export function disarmAlarm(deviceId: number): Promise<CommandResponse> {
  return apiPost<CommandResponse>('CommandService/DisarmAlarm', { DeviceId: deviceId });
}

// Live, one-shot, no PIN and no arm-state gating -- same request shape as arm/disarm, round-trips
// to the panel and waits for its ack. Deliberately no confirmation step before sending, matching
// the old app: these are emergency buttons, and slowing them down with a dialog defeats the point.
export function panic(deviceId: number): Promise<CommandResponse> {
  return apiPost<CommandResponse>('CommandService/Panic', { DeviceId: deviceId });
}

// Backend route is "Assault" (silent assault, FUNS) -- what this product calls "duress": a manual
// silent trigger, distinct from the loud/audible Panic button. There's no PIN-entry duress concept
// anywhere in this system (no separate duress code vs. real code); it's just this second button.
export function duress(deviceId: number): Promise<CommandResponse> {
  return apiPost<CommandResponse>('CommandService/Assault', { DeviceId: deviceId });
}

// Same shape/behavior as Panic/Assault above.
export function emergency(deviceId: number): Promise<CommandResponse> {
  return apiPost<CommandResponse>('CommandService/Emergency', { DeviceId: deviceId });
}

// Includes a ",FAIL" token in the response Text when any fault flag (AC/battery/bus/etc.) is
// set -- see CommandController.GetGeneralStatus. The dedicated failures screen (Day 18) reads
// the individual flags via a separate call; this is just the summary indicator.
export function getGeneralStatus(deviceId: number): Promise<CommandResponse> {
  return apiPost<CommandResponse>('CommandService/GetGeneralStatus', { DeviceId: deviceId });
}

// Raw analog power-supply readings (STSB) -- a distinct command from GetFailStatus below, not the
// same data. The backend types this request as InstallerCommandRequest, which requires a Command
// field even though this handler never reads it.
export function getBatteryStatus(deviceId: number): Promise<BatteryStateResponse> {
  return apiPost<BatteryStateResponse>('CommandService/GetBatteryStatus', { DeviceId: deviceId, Command: '' });
}

// The 10 discrete fault flags (STSF) that GetGeneralStatus's ",FAIL" token summarizes.
export function getFailStatus(deviceId: number): Promise<FailStatusResponse> {
  return apiPost<FailStatusResponse>('CommandService/GetFailStatus', { DeviceId: deviceId });
}

// Raw passthrough to the panel's own program-section protocol -- the backend prefixes "INST-"
// and the relay strips it back off before sending, accepting whatever the panel replies with
// (unlike every other command here, which waits for one specific response type). Role-gated
// server-side to Installer/Admin accounts (State === FORBIDDEN for EndUser).
export function sendInstallerCommand(deviceId: number, command: string): Promise<CommandResponse> {
  return apiPost<CommandResponse>('CommandService/SendInstallerCommand', { DeviceId: deviceId, Command: command });
}

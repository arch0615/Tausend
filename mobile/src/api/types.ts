// Mirrors Tausend.Core.Enums.ResponseStates -- see dashboard/src/api/types.ts, kept in
// sync by hand since the mobile app and the dashboard don't share a package.
export const ResponseState = {
  OK: 0,
  SERVER_ERROR: 1,
  BUSSINESS_ERROR: 2,
  UNAUTHORIZED: 3,
  WRONG_DATA: 4,
  NOT_FOUND: 5,
  CENTRAL_UNRESPONSIVE: 6,
  FORBIDDEN: 7,
} as const;
export type ResponseState = (typeof ResponseState)[keyof typeof ResponseState];

// Mirrors Tausend.Core.Enums.AccountRole
export const AccountRole = {
  EndUser: 0,
  Installer: 1,
  Admin: 2,
} as const;
export type AccountRole = (typeof AccountRole)[keyof typeof AccountRole];

export interface BaseResponse {
  State: ResponseState;
  Message: string;
  Code: number;
}

export interface AccountDevice {
  Description: string;
  Mac: string;
  Pin: string;
  DeviceId: number;
  IsOnline: boolean;
}

// SMS-connected panel -- a distinct shape from AccountDevice (IP panels): no Mac/IsOnline (no
// relay connection to report status), instead PhoneNumber/SimPin/DeviceType for the SMS command
// transport. See panels/PanelContext.tsx for how the two kinds are unified into one panel list.
export interface AccountSmsDevice {
  DeviceId: number;
  Description: string;
  Identifier: string;
  DevicePin: string;
  SimPin: string;
  PhoneNumber: string;
  DeviceType: string;
}

// CreateDevice's response -- Device is populated on success only.
export interface NewCreatedDeviceResponse extends BaseResponse {
  Device: AccountDevice | null;
}

// DissasociateCentral (route spelling preserved from the backend) removes only the caller's own
// AccountDevicePins row -- the panel and any other co-owners are untouched. IDOR-safe by
// construction (scoped to the caller's own AccessToken server-side), no extra fields returned.
export type DisassociateCentralResponse = BaseResponse;

// Arm/disarm/status/panic/etc. all return this shape. Text is not an enum -- it's a
// comma-joined token string (e.g. "ARM,AWAY,BELL", "READY", "" for no response from the panel).
// See panels/statusParser.ts for how it's interpreted, and CommandBusiness.GetStatus (backend)
// for where it's produced.
export interface CommandResponse extends BaseResponse {
  Text: string | null;
}

// GetFailStatus's 10 discrete fault flags -- a genuinely different command from GetBatteryStatus
// below, not the same data shown twice. GetGeneralStatus's ",FAIL" token is just a summary of
// "any of these is true", layered on top of this same response server-side.
export interface FailStatusResponse extends BaseResponse {
  AC: boolean;
  BAT: boolean;
  TLM: boolean;
  BELL1: boolean;
  VAUX: boolean;
  CLOCK: boolean;
  CEL: boolean;
  COMU: boolean;
  BUS: boolean;
  BELL2: boolean;
}

// Raw analog readings from the panel's power supply, in volts (Tension fields) and the panel's
// native current unit -- no percentage/charge-level field exists anywhere in the backend.
export interface BatteryStateResponse extends BaseResponse {
  InTension: number;
  ChargeTension: number;
  TestTension: number;
  Current: number;
}

// GetZones already resolves per-zone Open/Excluded server-side (against the panel, through the
// relay) -- the client never parses raw comma-joined zone strings itself. Always exactly 32
// entries (1..32); a zone with no saved name has Name === ZoneNumber.toString() (see ZoneBusiness.
// EnumZones on the backend, which synthesizes unnamed slots).
export interface Zone {
  ZoneId: number;
  DeviceId: number;
  ZoneNumber: number;
  Name: string;
  Excluded: boolean;
  Open: boolean;
}

export interface ListOfZonesResponse extends BaseResponse {
  Zones: Zone[] | null;
}

// A separate persisted entity from Zone (its own DB rows), but numbered 1:1 with zones -- the
// backend copies each zone's Name onto the matching exclusion at query time (DeviceController.
// GetExclusions), so Exclusion.Name always mirrors the zone label.
export interface Exclusion {
  ExclusionId: number;
  DeviceId: number;
  ExclusionNumber: number;
  Name: string;
  Excluded: boolean;
  Open: boolean;
}

export interface ListOfExclusionsResponse extends BaseResponse {
  Exclusions: Exclusion[] | null;
}

// Fixed count of 8 per device (unlike the 32 zones/exclusions), named the same way -- an unnamed
// slot has Name === ProgramControlNumber.toString(). GetProgramControls live-polls each output's
// on/off state from the panel (8 relay round-trips), so Activated is always fresh on read.
export interface ProgramControl {
  ProgramControlId: number;
  DeviceId: number;
  ProgramControlNumber: number;
  Name: string;
  Activated: boolean;
}

export interface ListOfProgramControlResponse extends BaseResponse {
  ProgramControls: ProgramControl[] | null;
}

// A recurring "at this time, on these days, set this PGM output to this state" rule -- fired
// server-side by the backend's ScheduledPgmDispatcher, not by this app. TimeOfDay is an
// "HH:MM:SS" string (System.Text.Json's default TimeSpan format, confirmed round-tripping as
// exactly this shape). DaysOfWeekMask bit N (0-6) means "fires on the day whose JS Date.getDay()
// value is N" (Sunday=0 .. Saturday=6) -- same convention as the backend's .NET DayOfWeek.
export interface ScheduledPgmAction {
  ScheduledPgmActionId: number;
  DeviceId: number;
  ProgramControlNumber: number;
  TimeOfDay: string;
  DaysOfWeekMask: number;
  DesiredState: boolean;
  Enabled: boolean;
}

export interface ListOfScheduledPgmActionsResponse extends BaseResponse {
  Schedules: ScheduledPgmAction[] | null;
}

export interface CreatedScheduledPgmActionResponse extends BaseResponse {
  ScheduledPgmActionId: number;
}

// PIN-holder labels (e.g. "Mom", "Housekeeper" for user number 3) -- purely cosmetic, DB-only.
// The panel is never told these names; it only knows PIN codes internally. Unlike Zones/PGM,
// EnumUsers returns only rows that already exist (open-ended UserNumber, not a fixed 1..N range),
// and there's no true delete -- "removing" a label means saving it with an empty UserName.
export interface PanelUser {
  UserId: number;
  DeviceId: number;
  UserNumber: number;
  UserName: string;
}

export interface ListOfUserResponse extends BaseResponse {
  Users: PanelUser[] | null;
}

// The Events SQL proc (EnumEvents.sql) only ever selects EventId/EventDateTime/Text -- every
// other field below comes back at its C# default (0/null) despite the DTO declaring them, so the
// UI only renders StringDate + Text, matching the old app. Named AlarmEvent, not Event, to avoid
// shadowing the DOM's global Event type.
export interface AlarmEvent {
  EventId: number;
  Secuence: number;
  EventDateTime: string;
  EventType: string | null;
  NotificationType: number;
  Partition: number;
  AlarmParameter: number;
  AlarmIdentifier: string | null;
  Text: string | null;
  StringDate: string | null;
}

export interface ListOfEventsResponse extends BaseResponse {
  Events: AlarmEvent[] | null;
}

// ISO-8601 strings -- new Date(DeviceTime) directly. ServerTime is the backend's own clock
// (already offset to match the panel's convention), not the phone's local time.
export interface TimeResponse extends BaseResponse {
  DeviceTime: string;
  ServerTime: string;
}

export interface Account {
  Email: string;
  FirstName: string;
  LastName: string;
  AccessToken: string;
  RefreshToken: string;
  Role: AccountRole;
  Devices: AccountDevice[];
  SmsDevices: AccountSmsDevice[];
  // Populated on login only, when the backend auto-unlinked one or more IP panels because their
  // saved PIN no longer validates against the panel (see AccountLoginResponse.PinChanged on the
  // backend) -- the previous app named these panels in a warning dialog right after login so the
  // user knows to re-link them, rather than just silently losing access.
  RemovedDevices: AccountDevice[];
}

export interface AccountLoginResponse extends BaseResponse {
  Account: Account | null;
  PinChanged: boolean;
}

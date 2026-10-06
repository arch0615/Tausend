// Mirrors Tausend.Core.Enums.ResponseStates
export const ResponseState = {
  OK: 0,
  SERVER_ERROR: 1,
  BUSSINESS_ERROR: 2,
  UNAUTHORIZED: 3,
  WRONG_DATA: 4,
  NOT_FOUND: 5,
  CENTRAL_UNRESPONSIVE: 6,
  FORBIDDEN: 7,
} as const
export type ResponseState = (typeof ResponseState)[keyof typeof ResponseState]

// Mirrors Tausend.Core.Enums.AccountRole
export const AccountRole = {
  EndUser: 0,
  Installer: 1,
  Admin: 2,
} as const
export type AccountRole = (typeof AccountRole)[keyof typeof AccountRole]

export interface BaseResponse {
  State: ResponseState
  Message: string
  Code: number
}

export interface AccountDevice {
  Description: string
  Mac: string
  Pin: string
  DeviceId: number
}

export interface Account {
  Email: string
  FirstName: string
  LastName: string
  AccessToken: string
  RefreshToken: string
  Role: AccountRole
  Devices: AccountDevice[]
}

export interface AccountLoginResponse extends BaseResponse {
  Account: Account | null
  PinChanged: boolean
}

export interface AdminAccountSummary {
  AccountId: number
  Email: string
  FirstName: string
  LastName: string
  Role: AccountRole
  Enabled: boolean
  CreatedDateTime: string
  LastLoginDateTime: string | null
}

export interface ListOfAdminAccountsResponse extends BaseResponse {
  Accounts: AdminAccountSummary[] | null
}

export interface AdminDeviceSummary {
  DeviceId: number
  Description: string
  Identifier: string
  Enabled: boolean
  IsOnline: boolean
  LastConnection: string | null
  CreatedDateTime: string
}

export interface ListOfAdminDevicesResponse extends BaseResponse {
  Devices: AdminDeviceSummary[] | null
}

// Mirrors Tausend.Core.Entities.Models.User -- a PIN-holder label/tag on a panel
// (e.g. user number 3 -> "Housekeeper"), not an Account.
export interface PanelUser {
  UserId: number
  UserNumber: number
  UserName: string
  DeviceId: number
}

export interface ListOfUserResponse extends BaseResponse {
  Users: PanelUser[] | null
}

// One AccountId<->DeviceId pair, mirroring backend-core's AccountDeviceLink model.
export interface AccountDeviceLink {
  AccountId: number
  DeviceId: number
}

export interface ListOfAccountDeviceLinksResponse extends BaseResponse {
  Links: AccountDeviceLink[] | null
}

// Live command responses -- mirrors mobile/src/api/types.ts's equivalents exactly, since these
// are the same CommandService/DeviceService endpoints called with an Admin's own token instead
// of the device owner's.
export interface CommandResponse extends BaseResponse {
  Text: string | null
}

export interface BatteryStateResponse extends BaseResponse {
  InTension: number
  ChargeTension: number
  TestTension: number
  Current: number
}

export interface FailStatusResponse extends BaseResponse {
  AC: boolean
  BAT: boolean
  TLM: boolean
  BELL1: boolean
  BELL2: boolean
  VAUX: boolean
  CLOCK: boolean
  CEL: boolean
  COMU: boolean
  BUS: boolean
}

export interface Zone {
  ZoneId: number
  DeviceId: number
  ZoneNumber: number
  Name: string
  Excluded: boolean
  Open: boolean
}

export interface ListOfZonesResponse extends BaseResponse {
  Zones: Zone[] | null
}

export interface ProgramControl {
  ProgramControlId: number
  DeviceId: number
  ProgramControlNumber: number
  Name: string | null
  Activated: boolean
}

export interface ListOfProgramControlResponse extends BaseResponse {
  ProgramControls: ProgramControl[] | null
}

export interface TimeResponse extends BaseResponse {
  DeviceTime: string
  ServerTime: string
}

// Mirrors backend-core's Event model. Events has no DeviceId/AccountId column of its own, only
// the panel's Identifier -- AlarmIdentifier is how the dashboard joins an event back to a device.
export interface AlarmEvent {
  EventId: number
  Secuence: number
  EventDateTime: string
  EventType: string | null
  NotificationType: number
  Partition: number
  AlarmParameter: number
  AlarmIdentifier: string | null
  Text: string | null
  StringDate: string | null
}

export interface ListOfEventsResponse extends BaseResponse {
  Events: AlarmEvent[] | null
}

export interface AuditLogEntry {
  AuditLogId: number
  ActorAccountId: number
  ActorEmail: string | null
  ActorFirstName: string | null
  ActorLastName: string | null
  Action: string
  TargetType: string
  TargetId: number | null
  Details: string | null
  CreatedDateTime: string
}

export interface ListOfAuditLogResponse extends BaseResponse {
  Entries: AuditLogEntry[] | null
}

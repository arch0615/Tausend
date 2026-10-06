import type { PanelKind } from '../panels/PanelContext';

export type AuthStackParamList = {
  Login: { email?: string; notice?: string } | undefined;
  Register: undefined;
  ForgotPassword: undefined;
};

// Home, ChangePassword, and the panel-selection screens are the only non-pairing screens
// through Day 13 -- Days 14-22 add the rest of the panel-management screens on top of this stack.
export type MainStackParamList = {
  Home: undefined;
  ChangePassword: undefined;
  SelectPairingType: undefined;
  PairingIntro: undefined;
  ConnectToPanel: undefined;
  CreateDevice: undefined;
  SmsPairing: undefined;
  PairingConfirmation: { path: 'wifi' | 'sms' };
  PanelSelector: undefined;
  ManagePanel: { kind: PanelKind; deviceId: number };
  PanelIdentification: undefined;
  Zones: undefined;
  Exclusions: undefined;
  Memory: undefined;
  Pgm: undefined;
  ScheduledDepartures: undefined;
  PanelUsers: undefined;
  Battery: undefined;
  Failures: undefined;
  Events: undefined;
  CustomMessages: undefined;
  ContactPhone: undefined;
  SmsPasswordChange: undefined;
  Clock: undefined;
  InstallerMode: undefined;
  Language: undefined;
  UserAccount: undefined;
};

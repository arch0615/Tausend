// Interprets the comma-joined token string CommandBusiness.GetStatus (backend) produces --
// ported from the previous app's getStatusMsg (home.page.ts), restructured as a plain data object
// instead of a pre-built label string so the RN screen can style each piece itself.
//
// Known tokens: READY, NOT-READY, ARM (+ one of STAY/NIGHT/AWAY, + NDLY), BELL, BYPASS, MEMO,
// FAIL (GetGeneralStatus only), ERROR, OK, or "" (empty -- the panel didn't respond; this is what
// a misspelled-but-load-bearing "DISCONECTED" collapses to server-side, see CommandBusiness.cs).
export type ArmMode = 'away' | 'stay' | 'night';

export interface ParsedStatus {
  raw: string;
  isEmpty: boolean; // no response from the panel
  isError: boolean;
  isReady: boolean;
  isNotReady: boolean;
  armMode: ArmMode | null;
  ndly: boolean;
  bell: boolean;
  bypass: boolean;
  memo: boolean;
  fail: boolean;
}

export function parseStatus(text: string): ParsedStatus {
  const tokens = text
    .split(',')
    .map((t) => t.trim())
    .filter(Boolean);

  return {
    raw: text,
    isEmpty: tokens.length === 0,
    isError: tokens.includes('ERROR'),
    isReady: tokens.includes('READY'),
    isNotReady: tokens.includes('NOT-READY'),
    armMode: tokens.includes('AWAY') ? 'away' : tokens.includes('STAY') ? 'stay' : tokens.includes('NIGHT') ? 'night' : null,
    ndly: tokens.includes('NDLY'),
    bell: tokens.includes('BELL'),
    bypass: tokens.includes('BYPASS'),
    memo: tokens.includes('MEMO'),
    fail: tokens.includes('FAIL'),
  };
}

// "Modo día" for STAY is intentional, not a mislabel -- DayArmAlarm sends the panel's STAY
// command, and the panel reports back STAY for what this product calls "day mode". Keep this
// mapping if you ever touch it.
export const ARM_MODE_LABEL: Record<ArmMode, string> = {
  away: 'Away',
  stay: 'Day',
  night: 'Night',
};

// Every raw token home.page.ts's getStatusMsg() can put in a comma-joined response, mapped to the
// i18n key for its human-readable phrase -- e.g. "ARM,AWAY,NDLY" becomes "Armed, Armed (away
// mode), Armed with no entry delay" (see New APP Videos/'s arm-confirmation toast). Deliberately
// separate from the status card's own labels (ARM_MODE_LABEL, describeStatus in HomeScreen.tsx):
// the toast always prefixes the mode with "Armed" ("Armada (modo día)"), the card shows the mode
// alone ("Modo Día") with "Armed" as its own small caption above the icon -- same tokens, two
// different phrasings depending on where they're shown.
export const STATUS_TOKEN_MESSAGE_KEY: Record<string, string> = {
  OK: 'Command sent successfully',
  READY: 'Ready to arm',
  'NOT-READY': 'Not ready, zones open',
  ARM: 'Armed',
  STAY: 'Armed (day mode)',
  NIGHT: 'Armed (night mode)',
  AWAY: 'Armed (away mode)',
  NDLY: 'Armed with no entry delay',
  BELL: 'Siren sounding',
  BYPASS: 'Zones excluded',
  MEMO: 'Memory',
  FAIL: 'Fault',
};

export function statusTokens(raw: string): string[] {
  return raw
    .split(',')
    .map((t) => t.trim())
    .filter(Boolean);
}

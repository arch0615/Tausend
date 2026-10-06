// Command composition for SMS-connected panels, ported from the previous (Ionic) app's working
// implementation (src/app/services/sms.service.ts) -- not documented in the panel's manufacturer
// spec sheets available in this repo. Every command is wrapped in the same envelope:
//   alarmas*<4-digit SIM PIN><command>*tausend
// with no separator between the PIN and the command body. The 3-letter opcodes mostly match the
// PRG/ARM/DAR-style vocabulary the TCP/relay path also uses (see backend/.../Enums/COMMANDS.cs),
// just carried inside this SMS envelope instead of the UDP framing.
//
// This module has no caller yet in this codebase (pairing an SMS device -- see
// screens/pairing/SmsPairingScreen.tsx -- only registers the panel's phone number/PIN with the
// backend and never itself sends a command); it exists so later screens that DO need to send
// runtime commands to SMS-connected panels (arm/disarm, panic/duress, etc.) have this ready
// rather than re-deriving the envelope format.

const ENVELOPE_PREFIX = 'alarmas*';
const ENVELOPE_SUFFIX = '*tausend';

function wrap(simPin: string, body: string): string {
  return `${ENVELOPE_PREFIX}${simPin}${body}${ENVELOPE_SUFFIX}`;
}

export const SmsCommands = {
  armAway: (simPin: string) => wrap(simPin, 'ARA'),
  armDay: (simPin: string) => wrap(simPin, 'ARD'),
  armNight: (simPin: string) => wrap(simPin, 'ARN'),
  disarm: (simPin: string) => wrap(simPin, 'DAR'),
  status: (simPin: string) => wrap(simPin, 'STS'),
  panic: (simPin: string) => wrap(simPin, 'PAN'),
  // "Duress" in this product's vocabulary -- a manual silent trigger, distinct from the audible
  // Panic button above. No PIN-entry duress concept exists in this system.
  duress: (simPin: string) => wrap(simPin, 'ASA'),
  // MED is the opcode the old app's "Emergencia" button sent over SMS.
  emergency: (simPin: string) => wrap(simPin, 'MED'),
  exclusions: (simPin: string, zones: number[]) => wrap(simPin, `exc${zones.join(',')}`),
  scheduledDeparture: (simPin: string, pgm: number) => wrap(simPin, `PGM${pgm}`),
  readContactNumber: (simPin: string, zone: number) => wrap(simPin, `PRG0${78 + zone}`),
  setContactNumber: (simPin: string, zone: number, number: string) => wrap(simPin, `PRG0${78 + zone}h${number}`),
  deleteContactNumber: (simPin: string, zone: number) => wrap(simPin, `PRG0${78 + zone}hf`),
  readMessage: (simPin: string, slot: number) => wrap(simPin, `SML${slot}`),
  setMessage: (simPin: string, slot: number, message: string) => wrap(simPin, `SMU${slot}${message}`),
  identification: (simPin: string, identification: string) => wrap(simPin, `IDF${identification}`),
  // Sent twice, matching the reference -- presumably the panel's own confirm-by-repetition rule.
  changePassword: (simPin: string, newPassword: string) => wrap(simPin, `PIN${newPassword}${newPassword}`),
};

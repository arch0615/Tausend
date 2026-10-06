import type { PanelUser } from '../api/types';

// Ported from the previous app's events.page.ts. Event text is generated server-side once, at
// the moment the event fires, and baked in with whatever the panel's description and any
// matching user label were AT THAT TIME (see backend-core's NotificationBusiness.SetUserName) --
// renaming the panel or saving a user label afterward doesn't retroactively rewrite old events.
// The previous app instead substitutes the CURRENTLY selected panel's name and CURRENTLY saved
// user labels into the displayed text every time it's shown, which is what people expect: the
// panel they renamed to "Alarma prueba" should read that way in every event, past and future.
export function applyEventTextSubstitutions(text: string, panelDescription: string, users: PanelUser[]): string {
  let result = text.replace(/\([^()]*\)/g, () => `(${panelDescription})`);
  result = result.replace(/'([^']*)'/g, (match, raw: string) => {
    if (raw === 'maestro') return 'maestro';
    const directNumber = Number(raw);
    const userNumber = raw.trim() !== '' && !Number.isNaN(directNumber) ? directNumber : Number(raw.split(':')[0]);
    if (Number.isNaN(userNumber)) return raw;
    const user = users.find((u) => u.UserNumber === userNumber);
    return user ? `${userNumber}: ${user.UserName}` : raw;
  });
  return result;
}

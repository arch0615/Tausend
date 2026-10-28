// White-label branding (client, 28 Oct): a stored logo's MimeType is whatever the picker actually
// detected (image/jpeg or image/png), never assumed -- a PNG rendered under a forced "image/jpeg"
// data URI loses its transparency, which breaks the exact thing a backgroundless logo is for (the
// client, 28 Oct: "if there is no background it is to take the app background screen"). Missing
// MimeType (a row saved before this field existed) falls back to JPEG, matching every logo stored
// before it.
export function logoDataUri(base64: string, mimeType?: string): string {
  return `data:${mimeType || 'image/jpeg'};base64,${base64}`;
}

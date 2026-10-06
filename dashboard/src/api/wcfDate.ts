// The WCF backend's DataContractJsonSerializer emits dates as
// "/Date(1783945809933+0200)/", which the native Date constructor can't parse
// at all. backend-core (System.Text.Json) emits plain ISO 8601 instead, which
// Date CAN parse natively -- this dashboard talks to either backend (see
// vite.config.ts), so both formats need to work here.
export function parseWcfDate(value: string | null | undefined): Date | null {
  if (!value) return null
  const wcfMatch = /\/Date\((-?\d+)/.exec(value)
  if (wcfMatch) return new Date(Number(wcfMatch[1]))
  const iso = new Date(value)
  return Number.isNaN(iso.getTime()) ? null : iso
}

// One voice everywhere: the platform system font (San Francisco / Roboto). Nothing here sets a
// fontFamily, so every string in the app renders in the same family.
//
// This used to be two voices -- Space Mono was reserved for the panel's "own" LCD-style voice on
// status readouts and mode names. On a real device that read as two different fonts on one
// screen ("Sirena activa" and "No lista (zonas abiertas)" in monospace against sans-serif
// everywhere else), which the client reported as issue #18: unify the fonts, the previous app
// used the plain system sans-serif. The readout styles below keep their own size, weight and
// letter-spacing so status text still stands out, just without the separate family.
//
// The Space Mono files are still bundled (src/assets/fonts, linked via react-native.config.js)
// but nothing references them any more; they can be dropped in a later cleanup.
export const typography = {
  title: { fontSize: 22, fontWeight: '800' as const, letterSpacing: -0.2 },
  body: { fontSize: 15, fontWeight: '400' as const },
  bodyDim: { fontSize: 13.5, fontWeight: '400' as const },
  label: { fontSize: 11, fontWeight: '700' as const, letterSpacing: 0.6, textTransform: 'uppercase' as const },
  button: { fontSize: 15, fontWeight: '700' as const, letterSpacing: 0.1 },
  // Status words, mode names ("ARMED", "READY"), zone numbers. Weight carries the emphasis that
  // the bold monospace face used to carry.
  readout: { fontSize: 20, fontWeight: '800' as const, letterSpacing: -0.2 },
  readoutSm: { fontSize: 11, fontWeight: '600' as const, letterSpacing: 0.4 },
};

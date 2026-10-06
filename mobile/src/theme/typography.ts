// Two voices: the platform system font (San Francisco / Roboto) carries everything a person
// reads -- titles, body copy, buttons -- and stays fontFamily-less so it always renders
// correctly with zero asset risk. Space Mono (bundled -- see src/assets/fonts, linked via
// react-native.config.js) is reserved for the panel's OWN voice: status readouts, mode names,
// zone/event timestamps -- the things a physical alarm keypad would print on its own LCD.
// Referenced by exact filename (SpaceMono-Regular / SpaceMono-Bold) -- Android resolves custom
// fontFamily values from assets/fonts/<name>.ttf this way, unlike CSS's family+weight matching.
export const typography = {
  title: { fontSize: 22, fontWeight: '800' as const, letterSpacing: -0.2 },
  body: { fontSize: 15, fontWeight: '400' as const },
  bodyDim: { fontSize: 13.5, fontWeight: '400' as const },
  label: { fontSize: 11, fontWeight: '700' as const, letterSpacing: 0.6, textTransform: 'uppercase' as const },
  button: { fontSize: 15, fontWeight: '700' as const, letterSpacing: 0.1 },
  // The panel's voice -- status words, mode names ("ARMED", "READY"), zone numbers.
  readout: { fontFamily: 'SpaceMono-Bold', fontSize: 20, letterSpacing: -0.2 },
  readoutSm: { fontFamily: 'SpaceMono-Regular', fontSize: 11, letterSpacing: 0.4 },
};

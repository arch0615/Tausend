// Matches New APP Videos/ (the client's own reference recordings for this app's target design,
// not the old Ionic app -- see memory/project_frontend_design_philosophy.md's 2026-09-12
// correction). Previously this app used a separate light "Open sky" palette that was approved
// under the mistaken belief those videos recorded the old app; this replaces it outright.
//
// The reference has no OS-driven light/dark variant -- one fixed identity always, matching how
// the legacy Ionic app also had a single fixed look. ThemeProvider no longer branches on
// useColorScheme() because of this -- see that file's comment.

export interface Palette {
  // Dark navy-to-purple vertical gradient behind every screen (see components/GradientBackground).
  gradientTop: string;
  gradientBottom: string;
  // Flat fallback equal to the gradient's midpoint -- for surfaces that can't render a gradient
  // (status bar color, Android nav bar, the MainMenu drawer sheet).
  bg: string;
  // White card/input/list-group surface -- distinct from headerBg below (the video's cards are
  // white even though the header bar above them is teal, so these can't share one token).
  panel: string;
  // Solid teal header bar (RootNavigator's navigationTheme.card, MainStack/AuthStack headerStyle).
  headerBg: string;
  // White header title/tint -- decoupled from `ink` below, which is for text on white cards and
  // would have no contrast against a teal header.
  headerInk: string;
  ink: string;
  inkDim: string;
  line: string;
  // Text sitting directly on a dark surface -- the gradient background itself, or the MainMenu
  // drawer sheet -- as opposed to `ink`/`inkDim`, which are tuned for text on white cards/inputs
  // and would have poor contrast used directly on gradientTop/gradientBottom/bg.
  onDark: string;
  onDarkDim: string;
  // Teal-green -- the "Lista Para Armar" ready state and the general primary-action color.
  accent: string;
  accentInk: string;
  // Coral-red -- armed/danger/emergency state and destructive actions.
  danger: string;
  dangerBg: string;
  okBg: string;
  okInk: string;
  // Distinct from danger/ok -- "caution, not a fault" (e.g. a bypassed zone). Ported from the
  // previous app's 4-state zone coloring (zones.page.ts getZoneColor): open=danger,
  // excluded=warning, excluded+open=alternating warning/danger, normal=inkDim/line.
  warning: string;
  warningBg: string;
  // The "ALARMAS TAUSEND" wordmark's own fixed brand colors (blue/red, from the project's
  // logo.jpeg) -- deliberately separate from accent/danger above, which serve functional UI
  // roles elsewhere and could diverge from the literal brand mark colors over time.
  brandBlue: string;
  brandRed: string;
}

export const palette: Palette = {
  gradientTop: '#123244',
  gradientBottom: '#1b0f36',
  bg: '#141c30',
  panel: '#ffffff',
  headerBg: '#1f5c6b',
  headerInk: '#ffffff',
  ink: '#173044',
  inkDim: '#5c7d8f',
  line: '#dbe6ea',
  onDark: '#f2f8fa',
  onDarkDim: '#a9c3ce',
  accent: '#2f9e86',
  accentInk: '#ffffff',
  danger: '#d9634f',
  dangerBg: '#fbe4df',
  okBg: '#e1f3ec',
  okInk: '#2f9e86',
  warning: '#e0a23a',
  warningBg: '#fbeed9',
  brandBlue: '#1a56c4',
  brandRed: '#d81e2c',
};

// Kept as named exports (rather than changing every `lightPalette`/`darkPalette` import site) so
// ThemeProvider's existing shape stays valid -- both now resolve to the one video-matched palette.
export const lightPalette: Palette = palette;
export const darkPalette: Palette = palette;

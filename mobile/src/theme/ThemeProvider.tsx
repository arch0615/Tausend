import { createContext, useContext, type ReactNode } from 'react';
import { palette, type Palette } from './colors';
import { spacing, radius } from './spacing';
import { typography } from './typography';

interface Theme {
  colors: Palette;
  spacing: typeof spacing;
  radius: typeof radius;
  typography: typeof typography;
  // Always true -- the video reference this app's visuals now match (New APP Videos/) has one
  // fixed dark-surfaced identity, not an OS-driven light/dark variant, so this no longer branches
  // on useColorScheme(). Kept as a field (rather than removed) since StatusDot's glow-opacity
  // still reads it, and a literal `true` is clearer at that call site than a bare boolean prop.
  dark: boolean;
}

const ThemeContext = createContext<Theme | null>(null);

export function ThemeProvider({ children }: { children: ReactNode }) {
  const theme: Theme = {
    colors: palette,
    spacing,
    radius,
    typography,
    dark: true,
  };
  return <ThemeContext.Provider value={theme}>{children}</ThemeContext.Provider>;
}

export function useTheme(): Theme {
  const ctx = useContext(ThemeContext);
  if (!ctx) throw new Error('useTheme must be used within ThemeProvider');
  return ctx;
}

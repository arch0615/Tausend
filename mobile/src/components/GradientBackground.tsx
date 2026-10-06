import type { ReactNode } from 'react';
import type { StyleProp, ViewStyle } from 'react-native';
import LinearGradient from 'react-native-linear-gradient';
import { useTheme } from '../theme/ThemeProvider';

// The dark navy-to-purple vertical gradient behind every screen in New APP Videos/ -- the client's
// own reference for this app's visual design (see memory/project_frontend_design_philosophy.md's
// 2026-09-12 correction). Used as each screen's root container in place of a flat
// `backgroundColor: colors.bg` View/ScrollView, matching the video's consistent full-screen
// gradient rather than a solid fill.
export function GradientBackground({ style, children }: { style?: StyleProp<ViewStyle>; children: ReactNode }) {
  const { colors } = useTheme();
  return (
    <LinearGradient colors={[colors.gradientTop, colors.gradientBottom]} style={style}>
      {children}
    </LinearGradient>
  );
}

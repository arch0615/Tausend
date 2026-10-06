import { Pressable, StyleSheet, Text, View } from 'react-native';
import { useTheme } from '../theme/ThemeProvider';

// New APP Videos/ shows a solid green bar pinned to the bottom of the screen after an action
// succeeds (arming/disarming on Home, renaming a PGM output, finishing the Access Point wizard),
// with the panel's own confirmation text and a manual "X" to dismiss -- not an inline Banner
// mixed into the scrolling content. Positioned absolutely so it floats over whatever's on screen.
export function Snackbar({ message, onDismiss }: { message: string | null; onDismiss: () => void }) {
  const { colors, typography, spacing, radius } = useTheme();
  if (!message) return null;
  return (
    <View style={[styles.container, { backgroundColor: colors.accent, borderRadius: radius.sm, padding: spacing.md }]}>
      <Text style={[typography.body, { color: colors.accentInk, flex: 1 }]}>{message}</Text>
      <Pressable onPress={onDismiss} hitSlop={10} style={{ marginLeft: spacing.md }}>
        <Text style={{ color: colors.accentInk, fontSize: 16, fontWeight: '700' }}>{'✕'}</Text>
      </Pressable>
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    position: 'absolute',
    left: 16,
    right: 16,
    bottom: 24,
    flexDirection: 'row',
    alignItems: 'center',
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 4 },
    shadowOpacity: 0.25,
    shadowRadius: 10,
    elevation: 6,
  },
});

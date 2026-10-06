import { Pressable, StyleSheet, Text, View } from 'react-native';
import { useTheme } from '../theme/ThemeProvider';

// The previous app repeated this same row -- a refresh action plus a tap-to-switch-panel
// button -- at the top of nearly every panel screen (Home, Zones, Exclusions, Clock, Battery,
// Memory, Events, etc). New APP Videos/'s Battery status.mp4 shows it as a white circular
// refresh icon button plus a bookmark-icon panel-name pill (matching Home's own TopBar in
// HomeScreen.tsx), not a text "Refresh" pill -- restyled to match both that and Home exactly.
export function PanelToolbar({
  description,
  onSwitchPanel,
  onRefresh,
  refreshing,
}: {
  description: string;
  onSwitchPanel: () => void;
  onRefresh?: () => void;
  refreshing?: boolean;
}) {
  const { colors, typography, radius } = useTheme();
  return (
    <View style={styles.row}>
      {onRefresh && (
        <Pressable
          onPress={onRefresh}
          disabled={refreshing}
          style={[styles.refreshCircle, { backgroundColor: colors.panel, opacity: refreshing ? 0.5 : 1 }]}
        >
          <Text style={{ fontSize: 16, color: colors.accent }}>{'⟳'}</Text>
        </Pressable>
      )}
      <Pressable
        onPress={onSwitchPanel}
        style={[styles.panelPill, { backgroundColor: colors.panel, borderRadius: radius.pill }]}
      >
        <Text style={{ fontSize: 15, marginRight: 8 }}>{'\u{1F516}'}</Text>
        <Text style={[typography.body, { color: colors.ink, fontWeight: '700' }]} numberOfLines={1}>
          {description}
        </Text>
      </Pressable>
    </View>
  );
}

const styles = StyleSheet.create({
  row: {
    flexDirection: 'row',
    alignItems: 'center',
  },
  refreshCircle: {
    width: 36,
    height: 36,
    borderRadius: 18,
    alignItems: 'center',
    justifyContent: 'center',
    marginRight: 12,
  },
  panelPill: {
    flex: 1,
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    paddingVertical: 8,
    paddingHorizontal: 14,
  },
});

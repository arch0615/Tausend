import type { ReactNode } from 'react';
import { Pressable, StyleSheet, Text, View } from 'react-native';
import { useTheme } from '../theme/ThemeProvider';

interface ListRowProps {
  /** Short glyph (1-2 characters, or a single emoji) shown in a small rounded tile on the
   * left -- deliberately not an icon-font dependency, see components/README notes below. */
  icon?: string;
  /** Background for the icon tile. Emoji are multi-color bitmap glyphs (their own rendered
   * color can't be overridden), so a uniformly dark tile made some of them nearly invisible --
   * every call site should pass a distinct, medium-brightness color so its glyph reads clearly
   * and neighboring rows stay visually distinguishable (see MainMenu.tsx's ICON_COLORS). Falls
   * back to colors.bg only so an old call site missing this prop doesn't crash, not because
   * that's a good default. */
  iconColor?: string;
  label: string;
  /** Right-aligned secondary text, e.g. a count or current value. */
  value?: string;
  onPress?: () => void;
  /** Hides the trailing chevron -- for rows that show a value/control instead of navigating. */
  navigates?: boolean;
  disabled?: boolean;
}

// The settings-menu pattern used across Panel settings, Panel users, Language, etc. --
// replaces a stack of identical full-width buttons with a grouped, scannable list. Rows in
// the same group should be wrapped in a single bordered container by the screen (see
// PanelSettingsScreen) with a 1px divider between them, matching a native settings list.
export function ListRow({ icon, iconColor, label, value, onPress, navigates = true, disabled }: ListRowProps) {
  const { colors, typography, spacing, radius } = useTheme();
  const content = (
    <View style={[styles.row, { paddingVertical: spacing.md, paddingHorizontal: spacing.md }]}>
      {icon && (
        <View
          style={[
            styles.iconTile,
            { backgroundColor: iconColor ?? colors.bg, borderRadius: radius.sm, marginRight: spacing.md },
          ]}
        >
          <Text style={{ fontSize: 14 }}>{icon}</Text>
        </View>
      )}
      <Text style={[typography.body, { color: colors.ink, flex: 1 }]}>{label}</Text>
      {value && (
        <Text style={[typography.bodyDim, { color: colors.inkDim, marginRight: spacing.xs }]}>{value}</Text>
      )}
      {onPress && navigates && <Text style={{ color: colors.inkDim, fontSize: 16 }}>{'›'}</Text>}
    </View>
  );

  if (!onPress) return content;

  return (
    <Pressable onPress={onPress} disabled={disabled} style={({ pressed }) => ({ opacity: pressed ? 0.6 : 1 })}>
      {content}
    </Pressable>
  );
}

/** Groups ListRows into a single bordered, dividing container -- the visual unit a settings
 * screen is actually built from. */
export function ListGroup({ children }: { children: ReactNode }) {
  const { colors, radius } = useTheme();
  const items = Array.isArray(children) ? children.filter(Boolean) : [children];
  return (
    // Shadow lives on this outer wrapper, not the clipped inner view below -- overflow:'hidden'
    // (needed there for the rounded-corner row dividers) would otherwise clip the shadow itself.
    <View style={[styles.groupShadow, { borderRadius: radius.md, shadowColor: '#000' }]}>
      <View style={[styles.group, { backgroundColor: colors.panel, borderColor: colors.line, borderRadius: radius.md }]}>
        {items.map((child, i) => (
          <View key={i}>
            {child}
            {i < items.length - 1 && <View style={{ height: 1, backgroundColor: colors.line }} />}
          </View>
        ))}
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  row: {
    flexDirection: 'row',
    alignItems: 'center',
  },
  iconTile: {
    width: 28,
    height: 28,
    alignItems: 'center',
    justifyContent: 'center',
  },
  group: {
    borderWidth: 1,
    overflow: 'hidden',
  },
  groupShadow: {
    shadowOffset: { width: 0, height: 4 },
    shadowOpacity: 0.06,
    shadowRadius: 10,
    elevation: 1,
    marginBottom: 4,
  },
});

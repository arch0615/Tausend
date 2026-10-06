import { Modal, Pressable, ScrollView, StyleSheet, Text, View } from 'react-native';
import { useNavigation } from '@react-navigation/native';
import { useTheme } from '../theme/ThemeProvider';
import { useLocale } from '../i18n/LocaleContext';
import { useAuth } from '../auth/AuthContext';
import { usePanels } from '../panels/PanelContext';
import { AccountRole } from '../api/types';
import { ListGroup, ListRow } from './ListRow';

const APP_VERSION = '1.0';

// Every icon tile used to share the same near-black background (colors.bg) -- fine for glyphs
// that happen to render bright, but several (⌂, 📡, ⏻) are naturally dark/muted and nearly
// disappeared against it. A dedicated, varied palette here guarantees contrast regardless of
// any one emoji's own coloring, and doubles as a way to tell rows apart at a glance. Danger-toned
// items (Fallas, unlink, log out) intentionally reuse colors.danger for its usual meaning;
// everything else just needs to differ from its immediate neighbors, not be globally unique.
const ICON_COLORS = {
  teal: '#2f9e86',
  blue: '#3b82f6',
  amber: '#f59e0b',
  violet: '#8b5cf6',
  pink: '#ec4899',
  cyan: '#06b6d4',
  orange: '#f97316',
  indigo: '#6366f1',
  lime: '#65a30d',
  slate: '#64748b',
};

// The previous app kept every section one tap away from anywhere via a persistent side-drawer
// menu (app.component.html) -- this app had navigation scattered across a Home-screen button
// list and a separate "Panel settings" hub screen you had to first navigate into. This modal,
// opened from a header button present on every screen (see MainStack.tsx), restores that
// "always reachable" property, without adding a new native drawer dependency this
// memory-constrained dev machine can't reliably build with.
//
// Section structure (CENTRAL IP / CONFIGURACIÓN / CENTRAL SMS / CUENTA) and the panel switcher
// pinned near the top are per the client's own explicit 2026-09-10 design brief -- see
// memory/project_frontend_design_philosophy.md. Keep this shape when adding new sections rather
// than inventing a new grouping.
export function MainMenu({ visible, onClose }: { visible: boolean; onClose: () => void }) {
  const { colors, typography, spacing, radius } = useTheme();
  const { t } = useLocale();
  const navigation = useNavigation();
  const { session, logout } = useAuth();
  const { panels, selected } = usePanels();
  const role = session?.account.Role;
  const isInstaller = role === AccountRole.Installer || role === AccountRole.Admin;

  function go(screen: string, params?: object) {
    onClose();
    // MainStack's own param list types this precisely at each call site (ListRow.onPress);
    // this helper is shared across many destinations so it stays loosely typed here.
    (navigation as any).navigate(screen, params);
  }

  return (
    <Modal visible={visible} animationType="slide" onRequestClose={onClose} transparent>
      <Pressable style={styles.backdrop} onPress={onClose} />
      <View style={[styles.sheet, { backgroundColor: colors.bg }]}>
        <ScrollView contentContainerStyle={{ padding: 24 }}>
          <Text style={[typography.label, { color: colors.onDarkDim, marginBottom: spacing.sm }]}>
            {t('ALARMAS TAUSEND 2.0')}
          </Text>

          <Pressable
            onPress={() => (panels.length > 0 ? go('PanelSelector') : go('SelectPairingType'))}
            style={[
              styles.panelSwitcher,
              { backgroundColor: colors.panel, borderColor: colors.line, borderRadius: radius.md, marginBottom: spacing.xl },
            ]}
          >
            <Text style={[typography.body, { color: colors.ink, fontWeight: '700', flex: 1 }]} numberOfLines={1}>
              {selected ? selected.description : t('Select a panel')}
            </Text>
            <Text style={{ color: colors.inkDim, fontSize: 14 }}>{'▾'}</Text>
          </Pressable>

          <ListGroup>
            <ListRow icon="⌂" iconColor={ICON_COLORS.teal} label={t('Home')} onPress={() => go('Home')} />
          </ListGroup>

          {selected && selected.kind === 'ip' && (
            <>
              <SectionLabel text={t('IP PANEL')} spacing={spacing} color={colors.onDarkDim} typography={typography} />
              <ListGroup>
                <ListRow icon="◫" iconColor={ICON_COLORS.blue} label={t('Zones')} onPress={() => go('Zones')} />
                <ListRow icon="⚠" iconColor={colors.danger} label={t('Failures')} onPress={() => go('Failures')} />
                <ListRow icon="≡" iconColor={ICON_COLORS.violet} label={t('Events')} onPress={() => go('Events')} />
                <ListRow icon="⏻" iconColor={ICON_COLORS.orange} label={t('PGM outputs')} onPress={() => go('Pgm')} />
                <ListRow icon="🗓" iconColor={ICON_COLORS.cyan} label={t('Scheduled departures')} onPress={() => go('ScheduledDepartures')} />
                <ListRow icon="👤" iconColor={ICON_COLORS.pink} label={t('User labels')} onPress={() => go('PanelUsers')} />
                <ListRow icon="🕐" iconColor={ICON_COLORS.indigo} label={t('Clock')} onPress={() => go('Clock')} />
                <ListRow icon="🔋" iconColor={ICON_COLORS.lime} label={t('Battery')} onPress={() => go('Battery')} />
              </ListGroup>
            </>
          )}

          <SectionLabel text={t('SETUP')} spacing={spacing} color={colors.onDarkDim} typography={typography} />
          <ListGroup>
            <ListRow icon="📡" iconColor={ICON_COLORS.blue} label={t('Access Point')} onPress={() => go('PairingIntro')} />
            {/* app.component.html's drawer has both "Vincular Equipo" (/setup -- add a panel)
                and "Selector de Equipo" (/selector -- switch/manage linked panels) as distinct
                items; neither was reachable from this menu before. */}
            <ListRow icon="➕" iconColor={ICON_COLORS.amber} label={t('Vincular Equipo')} onPress={() => go('SelectPairingType')} />
            {panels.length > 0 && (
              <ListRow icon="🔖" iconColor={ICON_COLORS.violet} label={t('Device selector')} onPress={() => go('PanelSelector')} />
            )}
            {isInstaller && <ListRow icon="🔧" iconColor={ICON_COLORS.slate} label={t('Installer mode')} onPress={() => go('InstallerMode')} />}
            {selected && (
              <ListRow
                icon="⎋"
                iconColor={colors.danger}
                label={t('Unlink or lock this panel')}
                onPress={() => go('ManagePanel', { kind: selected.kind, deviceId: selected.deviceId })}
              />
            )}
          </ListGroup>

          {selected && selected.kind === 'sms' && (
            <>
              <SectionLabel text={t('SMS PANEL')} spacing={spacing} color={colors.onDarkDim} typography={typography} />
              <ListGroup>
                <ListRow icon="✎" iconColor={ICON_COLORS.teal} label={t('Identify panel')} onPress={() => go('PanelIdentification')} />
                <ListRow icon="⏻" iconColor={ICON_COLORS.orange} label={t('PGM outputs')} onPress={() => go('Pgm')} />
                <ListRow icon="💬" iconColor={ICON_COLORS.pink} label={t('Custom messages')} onPress={() => go('CustomMessages')} />
                <ListRow icon="📞" iconColor={ICON_COLORS.cyan} label={t('Contact numbers')} onPress={() => go('ContactPhone')} />
                <ListRow icon="🔑" iconColor={ICON_COLORS.amber} label={t('Change SMS PIN')} onPress={() => go('SmsPasswordChange')} />
              </ListGroup>
            </>
          )}

          <SectionLabel text={t('ACCOUNT')} spacing={spacing} color={colors.onDarkDim} typography={typography} />
          <ListGroup>
            <ListRow icon="🔑" iconColor={ICON_COLORS.amber} label={t('Change password')} onPress={() => go('ChangePassword')} />
            <ListRow icon="🌐" iconColor={ICON_COLORS.blue} label={t('Language')} onPress={() => go('Language')} />
            {/* Previous app's Ajustes -> "Cuenta de usuario" -- shows account details and the
                "Eliminar Cuenta" action; see UserAccountScreen.tsx. */}
            <ListRow icon="👤" iconColor={ICON_COLORS.slate} label={t('User account')} onPress={() => go('UserAccount')} />
            <ListRow icon="⏻" iconColor={colors.danger} label={t('Log out')} navigates={false} onPress={() => { onClose(); logout(); }} />
          </ListGroup>

          <Text style={[typography.bodyDim, { color: colors.onDarkDim, textAlign: 'center', marginTop: spacing.xl }]}>
            {t('Version {v}', { v: APP_VERSION })}
          </Text>
        </ScrollView>
      </View>
    </Modal>
  );
}

function SectionLabel({
  text,
  spacing,
  color,
  typography,
}: {
  text: string;
  spacing: ReturnType<typeof useTheme>['spacing'];
  color: string;
  typography: ReturnType<typeof useTheme>['typography'];
}) {
  return (
    <Text style={[typography.label, { color, marginTop: spacing.xl, marginBottom: spacing.sm }]}>{text}</Text>
  );
}

export function MenuButton({ onPress }: { onPress: () => void }) {
  const { colors } = useTheme();
  return (
    <Pressable onPress={onPress} hitSlop={12} style={{ paddingHorizontal: 8, paddingVertical: 4 }}>
      <Text style={{ fontSize: 20, color: colors.headerInk }}>{'☰'}</Text>
    </Pressable>
  );
}

const styles = StyleSheet.create({
  backdrop: {
    flex: 1,
    backgroundColor: 'rgba(0,0,0,0.35)',
  },
  sheet: {
    position: 'absolute',
    top: 0,
    bottom: 0,
    left: 0,
    width: '82%',
    maxWidth: 340,
  },
  panelSwitcher: {
    flexDirection: 'row',
    alignItems: 'center',
    borderWidth: 1,
    padding: 16,
  },
});

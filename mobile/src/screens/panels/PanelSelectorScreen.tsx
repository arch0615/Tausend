import { useCallback, useState } from 'react';
import { Image, KeyboardAvoidingView, Platform, Pressable, ScrollView, StyleSheet, Text, View } from 'react-native';
import { useFocusEffect } from '@react-navigation/native';
import { useTheme } from '../../theme/ThemeProvider';
import { GradientBackground } from '../../components/GradientBackground';
import { Card } from '../../components/Card';
import { StatusDot } from '../../components/StatusDot';
import { useLocale } from '../../i18n/LocaleContext';
import { usePanels, type Panel } from '../../panels/PanelContext';
import { getGeneralStatus } from '../../api/command';
import { panelAnswered } from '../../panels/commandFeedback';
import { FormField } from '../../components/FormField';
import { PrimaryButton } from '../../components/PrimaryButton';
import type { MainStackParamList } from '../../navigation/types';

// A lighter probe than HomeScreen's fetchStatus (STATUS_MAX_ATTEMPTS=4 there): this runs once per
// IP panel on the list, in parallel across however many panels there are, so it needs to settle
// quickly rather than ride out a flaky exchange the way the single focused panel's own screen does.
// Two tries is enough to not mistake one dropped UDP packet for an offline panel without making a
// long list feel slow.
const PROBE_ATTEMPTS = 2;
const PROBE_RETRY_DELAY_MS = 250;

function delay(ms: number): Promise<void> {
  return new Promise((resolve) => setTimeout(resolve, ms));
}

// Same asset as Login/Home's eagle mark -- New APP Videos/'s "Selector de Equipo" screen also
// centers it above a white card, overlapping the card's top edge, rather than a plain page title.
const EAGLE_LOGO = require('../../assets/images/eagle-logo.png');


// HomeScreen renders this inline when an account has several panels and none is chosen yet, so
// the navigation prop is typed by the three destinations actually used rather than by this
// screen's own route -- Home's navigator prop is bound to "Home" and would not otherwise fit.
interface SelectorNavigation {
  navigate: {
    (screen: 'Home' | 'SelectPairingType'): void;
    (screen: 'ManagePanel', params: MainStackParamList['ManagePanel']): void;
  };
}

export function PanelSelectorScreen({ navigation }: { navigation: SelectorNavigation }) {
  const { colors, typography, spacing, radius } = useTheme();
  const { t } = useLocale();
  const { panels, selected, selectPanel, reachabilityOf, reportReachability } = usePanels();
  const [search, setSearch] = useState('');

  // Client, 22 Oct: "it is necessary that this screen shows the status condition directly"
  // instead of requiring a visit to each panel first -- reachabilityOf only gets filled in by a
  // screen that actually sends a command (HomeScreen's fetchStatus), so a panel nobody has opened
  // yet still falls back to the stale login-time isOnline flag here. This probes every IP panel on
  // the list itself, in parallel, so the real state is known before the user taps into any of them.
  useFocusEffect(
    useCallback(() => {
      let cancelled = false;
      for (const panel of panels) {
        if (panel.kind !== 'ip') continue;
        (async () => {
          for (let attempt = 0; attempt < PROBE_ATTEMPTS; attempt++) {
            if (cancelled) return;
            try {
              const res = await getGeneralStatus(panel.deviceId);
              if (panelAnswered(res)) {
                if (!cancelled) reportReachability('ip', panel.deviceId, true);
                return;
              }
            } catch {
              // fall through to retry/offline below
            }
            if (attempt < PROBE_ATTEMPTS - 1) await delay(PROBE_RETRY_DELAY_MS);
          }
          if (!cancelled) reportReachability('ip', panel.deviceId, false);
        })();
      }
      return () => {
        cancelled = true;
      };
    }, [panels, reportReachability]),
  );

  // Client, 22 Oct: "the screen must be sorted alphabetically by device name" -- applied after the
  // search filter so a search still narrows the same alphabetical list rather than its own order.
  const filtered = panels
    .filter((p) => p.description.toLowerCase().includes(search.trim().toLowerCase()))
    .sort((a, b) => a.description.localeCompare(b.description, undefined, { sensitivity: 'base' }));

  function handleSelect(panel: Panel) {
    selectPanel(panel);
    navigation.navigate('Home');
  }

  return (
    <GradientBackground style={{ flex: 1 }}>
    <KeyboardAvoidingView
      style={{ flex: 1 }}
      behavior={Platform.OS === 'ios' ? 'padding' : 'height'}
    >
      <ScrollView contentContainerStyle={styles.container} keyboardShouldPersistTaps="handled">
        <View style={styles.logoWrap}>
          <Image source={EAGLE_LOGO} style={styles.logo} resizeMode="contain" />
        </View>
        <Card style={styles.selectorCard}>
          <Text style={[styles.cardTitle, { color: colors.ink }]}>{t('Device selector')}</Text>

          {filtered.length === 0 ? (
            <Text style={[typography.bodyDim, { color: colors.inkDim, textAlign: 'center', paddingVertical: spacing.lg }]}>
              {panels.length === 0 ? t("You don't have any panels linked yet.") : t('No panels match your search.')}
            </Text>
          ) : (
            filtered.map((item) => {
              const isSelected = selected?.kind === item.kind && selected?.deviceId === item.deviceId;
              return (
                <View
                  key={`${item.kind}:${item.deviceId}`}
                  style={[
                    styles.row,
                    { borderColor: isSelected ? colors.accent : colors.line, borderRadius: radius.md, marginBottom: spacing.sm },
                  ]}
                >
                  {/* Sibling, not nested, Pressables -- two touchables inside one another have
                      inconsistent responder-negotiation behavior across platforms/RN versions. */}
                  <Pressable onPress={() => handleSelect(item)} style={styles.rowMain} hitSlop={8}>
                    <Text style={styles.rowIcon}>{'🔖'}</Text>
                    <View style={{ flex: 1 }}>
                      <Text style={[typography.body, { color: colors.ink, fontWeight: '700' }]} numberOfLines={1}>
                        {item.description}
                      </Text>
                      {item.kind === 'sms' && (
                        <Text style={[typography.bodyDim, { color: colors.inkDim, marginTop: 2 }]}>{t('SMS panel')}</Text>
                      )}
                    </View>
                    {/* Client, 22 Oct: a colour the screen shows at a glance, not text you have to
                        read -- replaces the old "Online"/"Offline" label on this row. Same
                        reasoning as HomeScreen's own indicator: prefer what a live round-trip just
                        proved over the login-time flag, which is what made this row say "Online"
                        with the Wi-Fi off. Unlike HomeScreen's muted dim-grey "not connected"
                        treatment, this one uses the client's own red/green -- their mockup asked
                        for it explicitly on this screen. */}
                    {item.kind === 'ip' &&
                      (() => {
                        const live = reachabilityOf('ip', item.deviceId);
                        const connected = live === 'unknown' ? !!item.isOnline : live === 'online';
                        return (
                          <View style={styles.connectionDot}>
                            <StatusDot color={connected ? colors.okInk : colors.danger} size={22} glow={false} />
                          </View>
                        );
                      })()}
                  </Pressable>
                  <View style={styles.rowEnd}>
                    {isSelected && (
                      <Text style={[typography.bodyDim, { color: colors.accent, marginBottom: 2 }]}>{t('Selected')}</Text>
                    )}
                    <Pressable
                      onPress={() => navigation.navigate('ManagePanel', { kind: item.kind, deviceId: item.deviceId })}
                      hitSlop={8}
                    >
                      <Text style={[styles.chevron, { color: colors.inkDim }]}>{'›'}</Text>
                    </Pressable>
                  </View>
                </View>
              );
            })
          )}
        </Card>

        {panels.length > 0 && (
          // Ordered below the list and above "Add a panel", matching the previous app's selector
          // (search + add were both fixed at the bottom, the list scrolled above them) -- not a
          // functional change, just restoring the familiar top-to-bottom layout.
          <View style={{ marginTop: spacing.lg }}>
            <FormField labelColor={colors.onDarkDim} label={t('Search')} value={search} onChangeText={setSearch} autoCapitalize="none" autoCorrect={false} />
          </View>
        )}

        <View style={{ marginTop: spacing.md }}>
          <PrimaryButton title={t('Add equipment')} onPress={() => navigation.navigate('SelectPairingType')} />
        </View>
      </ScrollView>
    </KeyboardAvoidingView>
    </GradientBackground>
  );
}

const styles = StyleSheet.create({
  container: {
    flexGrow: 1,
    padding: 24,
  },
  // zIndex alone doesn't win on Android: Card's own `elevation` (see components/Card.tsx) makes
  // Android paint it above a merely-higher-zIndex sibling that has no elevation of its own, so the
  // card's white background was covering the eagle's lower half. elevation here has to beat
  // Card's (2) for the logo to actually stay on top -- see LoginScreen.tsx's identical fix.
  logoWrap: {
    alignItems: 'center',
    marginBottom: -28,
    zIndex: 10,
    elevation: 6,
  },
  logo: {
    width: 140,
    height: 140,
  },
  selectorCard: {
    paddingTop: 40,
  },
  cardTitle: {
    fontSize: 18,
    fontWeight: '800',
    textAlign: 'center',
    textTransform: 'uppercase',
    marginBottom: 16,
  },
  row: {
    flexDirection: 'row',
    alignItems: 'center',
    borderWidth: 1,
    padding: 12,
  },
  rowMain: {
    flex: 1,
    flexDirection: 'row',
    alignItems: 'center',
  },
  rowIcon: {
    fontSize: 20,
    marginRight: 10,
  },
  connectionDot: {
    marginLeft: 12,
  },
  rowEnd: {
    alignItems: 'flex-end',
    marginLeft: 8,
  },
  chevron: {
    fontSize: 20,
  },
});

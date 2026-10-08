import { useState } from 'react';
import { Image, KeyboardAvoidingView, Platform, Pressable, ScrollView, StyleSheet, Text, View } from 'react-native';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import { useTheme } from '../../theme/ThemeProvider';
import { GradientBackground } from '../../components/GradientBackground';
import { Card } from '../../components/Card';
import { useLocale } from '../../i18n/LocaleContext';
import { usePanels, type Panel } from '../../panels/PanelContext';
import { FormField } from '../../components/FormField';
import { PrimaryButton } from '../../components/PrimaryButton';
import type { MainStackParamList } from '../../navigation/types';

// Same asset as Login/Home's eagle mark -- New APP Videos/'s "Selector de Equipo" screen also
// centers it above a white card, overlapping the card's top edge, rather than a plain page title.
const EAGLE_LOGO = require('../../assets/images/eagle-logo.png');

type Props = NativeStackScreenProps<MainStackParamList, 'PanelSelector'>;

export function PanelSelectorScreen({ navigation }: Props) {
  const { colors, typography, spacing, radius } = useTheme();
  const { t } = useLocale();
  const { panels, selected, selectPanel, reachabilityOf } = usePanels();
  const [search, setSearch] = useState('');

  const filtered = panels.filter((p) => p.description.toLowerCase().includes(search.trim().toLowerCase()));

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
                      <Text style={[typography.bodyDim, { color: colors.inkDim, marginTop: 2 }]}>
                        {item.kind === 'sms'
                          ? t('SMS panel')
                          : (() => {
                              // Same reasoning as HomeScreen's indicator: prefer what a real
                              // command round-trip proved this session over the login-time flag,
                              // which is what made this row read "Online" with the Wi-Fi off.
                              const live = reachabilityOf('ip', item.deviceId);
                              const connected = live === 'unknown' ? !!item.isOnline : live === 'online';
                              return connected ? t('Online') : t('Offline');
                            })()}
                      </Text>
                    </View>
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
  rowEnd: {
    alignItems: 'flex-end',
    marginLeft: 8,
  },
  chevron: {
    fontSize: 20,
  },
});

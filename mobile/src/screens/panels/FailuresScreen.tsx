import { useCallback, useState } from 'react';
import { ActivityIndicator, FlatList, StyleSheet, Text, View } from 'react-native';
import { useFocusEffect } from '@react-navigation/native';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import { useTheme } from '../../theme/ThemeProvider';
import { GradientBackground } from '../../components/GradientBackground';
import { useLocale } from '../../i18n/LocaleContext';
import { useAuth } from '../../auth/AuthContext';
import { usePanels } from '../../panels/PanelContext';
import { getFailStatus } from '../../api/command';
import { ResponseState, type FailStatusResponse } from '../../api/types';
import { PrimaryButton } from '../../components/PrimaryButton';
import { Banner } from '../../components/Banner';
import { PanelToolbar } from '../../components/PanelToolbar';
import type { MainStackParamList } from '../../navigation/types';

// Descriptions ported from the old app's failures.service.ts, translated to English.
const FAULT_DESCRIPTIONS: Record<keyof Omit<FailStatusResponse, 'State' | 'Message' | 'Code'>, string> = {
  AC: 'Main power (220VAC) supply failure',
  BAT: 'Battery failure (too low or faulty)',
  TLM: 'Phone line failure',
  BELL1: 'Siren 1 failure',
  BELL2: 'Siren 2 failure',
  VAUX: '12V auxiliary power failure (keypads/accessories)',
  CLOCK: "Clock failure (lost the panel's time setting)",
  CEL: 'Cellular module failure',
  COMU: 'Event communication failure',
  BUS: 'Keypad/accessory bus communication failure',
};

const FAULT_KEYS = Object.keys(FAULT_DESCRIPTIONS) as (keyof typeof FAULT_DESCRIPTIONS)[];

type Props = NativeStackScreenProps<MainStackParamList, 'Failures'>;

export function FailuresScreen({ navigation }: Props) {
  const { colors, typography, spacing, radius } = useTheme();
  const { t } = useLocale();
  const { logout } = useAuth();
  const { selected } = usePanels();
  const deviceId = selected?.kind === 'ip' ? selected.deviceId : null;

  const [faults, setFaults] = useState<FailStatusResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(async () => {
    if (!deviceId) return;
    setLoading(true);
    setError(null);
    try {
      const res = await getFailStatus(deviceId);
      if (res.State === ResponseState.UNAUTHORIZED) {
        // No manual navigation needed -- RootNavigator swaps to the login stack as soon as
        // session clears (see navigation/RootNavigator.tsx).
        logout();
        return;
      }
      if (res.State === ResponseState.CENTRAL_UNRESPONSIVE) {
        setError(t("The panel isn't responding right now."));
        return;
      }
      if (res.State !== ResponseState.OK) {
        setError(res.Message || t('Could not read failure status.'));
        return;
      }
      setFaults(res);
    } catch {
      setError(t('Could not reach the server.'));
    } finally {
      setLoading(false);
    }
  }, [deviceId, logout, t]);

  useFocusEffect(
    useCallback(() => {
      load();
    }, [load]),
  );

  if (!deviceId) {
    return (
      <GradientBackground style={styles.centered}>
        <Text style={[typography.bodyDim, { color: colors.onDarkDim }]}>{t('Select an IP panel to view its failures.')}</Text>
      </GradientBackground>
    );
  }

  const activeFaults = faults ? FAULT_KEYS.filter((key) => faults[key]) : [];

  return (
    <GradientBackground style={styles.container}>
      <View style={{ padding: 16, paddingBottom: 0 }}>
        <PanelToolbar
          description={selected?.description ?? ''}
          onSwitchPanel={() => navigation.navigate('PanelSelector')}
          onRefresh={load}
          refreshing={loading}
        />
      </View>

      {error && (
        <View style={{ padding: 16, paddingBottom: 0 }}>
          <Banner kind="error">{error}</Banner>
        </View>
      )}

      {loading ? (
        <ActivityIndicator style={{ marginTop: 32 }} size="large" color={colors.accent} />
      ) : activeFaults.length === 0 ? (
        <Text style={[typography.bodyDim, { color: colors.onDarkDim, textAlign: 'center', marginTop: 32 }]}>
          {t('No active faults.')}
        </Text>
      ) : (
        <FlatList
          data={activeFaults}
          keyExtractor={(key) => key}
          contentContainerStyle={{ padding: 16 }}
          renderItem={({ item }) => (
            <View
              style={[
                styles.row,
                { borderColor: colors.danger, backgroundColor: colors.dangerBg, borderRadius: radius.sm, marginBottom: spacing.sm },
              ]}
            >
              <Text style={[typography.label, { color: colors.danger, fontWeight: '700', marginBottom: 2 }]}>{item}</Text>
              <Text style={[typography.body, { color: colors.danger }]}>{t(FAULT_DESCRIPTIONS[item])}</Text>
            </View>
          )}
        />
      )}

      <View style={{ padding: 16, paddingTop: 0 }}>
        <View style={styles.bottomRow}>
          <View style={{ flex: 1, marginRight: spacing.sm }}>
            <PrimaryButton title={t('Refresh')} onPress={load} loading={loading} />
          </View>
          <View style={{ flex: 1 }}>
            <PrimaryButton title={t('Back')} onPress={() => navigation.goBack()} />
          </View>
        </View>
      </View>
    </GradientBackground>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
  },
  centered: {
    flex: 1,
    alignItems: 'center',
    justifyContent: 'center',
    padding: 24,
  },
  row: {
    borderWidth: 1,
    padding: 12,
  },
  bottomRow: {
    flexDirection: 'row',
  },
});

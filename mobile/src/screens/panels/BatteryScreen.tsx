import { useCallback, useState } from 'react';
import { ActivityIndicator, StyleSheet, Text, View } from 'react-native';
import { useFocusEffect } from '@react-navigation/native';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import { useTheme } from '../../theme/ThemeProvider';
import { GradientBackground } from '../../components/GradientBackground';
import { Card } from '../../components/Card';
import { useLocale } from '../../i18n/LocaleContext';
import { useAuth } from '../../auth/AuthContext';
import { usePanels } from '../../panels/PanelContext';
import { getBatteryStatus } from '../../api/command';
import { ResponseState, type BatteryStateResponse } from '../../api/types';
import { Banner } from '../../components/Banner';
import { PanelToolbar } from '../../components/PanelToolbar';
import type { MainStackParamList } from '../../navigation/types';

type Props = NativeStackScreenProps<MainStackParamList, 'Battery'>;

// Raw analog readings, not a charge percentage -- the panel doesn't report one. Matches the old
// app's plain readout list, no thresholds/colors (the backend never flags a reading as abnormal).
const ROWS: { key: keyof BatteryStateResponse; label: string; unit: string }[] = [
  { key: 'InTension', label: 'Input voltage', unit: 'V' },
  { key: 'ChargeTension', label: 'Charge voltage', unit: 'V' },
  { key: 'Current', label: 'Charge current', unit: 'mA' },
  { key: 'TestTension', label: 'Discharge voltage', unit: 'V' },
];

export function BatteryScreen({ navigation }: Props) {
  const { colors, typography } = useTheme();
  const { t } = useLocale();
  const { logout } = useAuth();
  const { selected } = usePanels();
  const deviceId = selected?.kind === 'ip' ? selected.deviceId : null;

  const [battery, setBattery] = useState<BatteryStateResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(async () => {
    if (!deviceId) return;
    setLoading(true);
    setError(null);
    try {
      const res = await getBatteryStatus(deviceId);
      if (res.State === ResponseState.UNAUTHORIZED) {
        // No manual navigation needed -- RootNavigator swaps to the login stack as soon as
        // session clears (see navigation/RootNavigator.tsx).
        logout();
        return;
      }
      if (res.State !== ResponseState.OK) {
        setError(res.Message || t('Could not read battery status.'));
        return;
      }
      setBattery(res);
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
        <Text style={[typography.bodyDim, { color: colors.onDarkDim }]}>{t('Select a Wi-Fi panel to view its battery status.')}</Text>
      </GradientBackground>
    );
  }

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
      ) : (
        <View style={{ padding: 16 }}>
          <Card style={styles.card}>
            {ROWS.map((row, i) => (
              <View
                key={row.key}
                style={[
                  styles.row,
                  i < ROWS.length - 1 && { borderBottomWidth: 1, borderBottomColor: colors.line },
                ]}
              >
                <Text style={[typography.body, { color: colors.ink, flex: 1 }]}>{t(row.label)}</Text>
                <Text style={[typography.body, { color: colors.inkDim }]}>
                  {battery ? `${battery[row.key]} ${row.unit}` : row.unit}
                </Text>
              </View>
            ))}
          </Card>
        </View>
      )}
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
  card: {
    padding: 0,
  },
  row: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingVertical: 14,
    paddingHorizontal: 16,
  },
});

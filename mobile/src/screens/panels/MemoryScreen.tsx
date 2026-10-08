import { useCallback, useEffect, useState } from 'react';
import { ActivityIndicator, FlatList, StyleSheet, Text, View } from 'react-native';
import { useFocusEffect } from '@react-navigation/native';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import { useTheme } from '../../theme/ThemeProvider';
import { GradientBackground } from '../../components/GradientBackground';
import { useLocale } from '../../i18n/LocaleContext';
import { useAuth } from '../../auth/AuthContext';
import { usePanels } from '../../panels/PanelContext';
import { getMemory } from '../../api/memory';
import { ResponseState, type Zone } from '../../api/types';
import { Banner } from '../../components/Banner';
import { PanelToolbar } from '../../components/PanelToolbar';
import type { MainStackParamList } from '../../navigation/types';

type Props = NativeStackScreenProps<MainStackParamList, 'Memory'>;

const BLINK_INTERVAL_MS = 1000;

// Read-only -- this is the panel's memory of which zones triggered since the last arm/reset, not
// a renameable list and not the timestamped event log (that's Day 20's Events screen).
export function MemoryScreen({ navigation }: Props) {
  const { colors, typography, spacing, radius } = useTheme();
  const { t } = useLocale();
  const { logout } = useAuth();
  const { selected } = usePanels();
  const deviceId = selected?.kind === 'ip' ? selected.deviceId : null;

  const [zones, setZones] = useState<Zone[] | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [blinkOn, setBlinkOn] = useState(true);

  const load = useCallback(async () => {
    if (!deviceId) return;
    setLoading(true);
    setError(null);
    try {
      const res = await getMemory(deviceId);
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
      setZones(res.Zones ?? []);
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

  // Currently-open memory zones blink red/yellow, matching the previous app's treatment.
  useEffect(() => {
    const interval = setInterval(() => setBlinkOn((v) => !v), BLINK_INTERVAL_MS);
    return () => clearInterval(interval);
  }, []);

  if (!deviceId) {
    return (
      <GradientBackground style={styles.centered}>
        <Text style={[typography.bodyDim, { color: colors.onDarkDim }]}>{t('Select an IP panel to view its memory.')}</Text>
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
      ) : (zones ?? []).length === 0 ? (
        <Text style={[typography.bodyDim, { color: colors.onDarkDim, textAlign: 'center', marginTop: 32 }]}>
          {t('No zones currently have memory.')}
        </Text>
      ) : (
        <FlatList
          data={zones ?? []}
          keyExtractor={(z) => String(z.ZoneNumber)}
          contentContainerStyle={{ padding: 16 }}
          renderItem={({ item }) => {
            const dotColor = item.Open ? (blinkOn ? colors.warning : colors.danger) : colors.warning;
            const name = item.Name === item.ZoneNumber.toString() ? t('Zone {n}', { n: item.ZoneNumber }) : item.Name;
            return (
              <View style={[styles.row, { backgroundColor: colors.panel, borderColor: colors.line, borderRadius: radius.sm, marginBottom: spacing.sm }]}>
                <View style={[styles.dot, { backgroundColor: dotColor }]} />
                <Text style={[typography.body, { color: colors.ink, flex: 1 }]} numberOfLines={1}>
                  {name}
                </Text>
              </View>
            );
          }}
        />
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
  row: {
    flexDirection: 'row',
    alignItems: 'center',
    borderWidth: 1,
    padding: 12,
  },
  dot: {
    width: 10,
    height: 10,
    borderRadius: 5,
    marginRight: 12,
  },
});

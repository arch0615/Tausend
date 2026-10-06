import { useCallback, useState } from 'react';
import { ActivityIndicator, FlatList, StyleSheet, Text, View } from 'react-native';
import { useFocusEffect } from '@react-navigation/native';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import { useTheme } from '../../theme/ThemeProvider';
import { GradientBackground } from '../../components/GradientBackground';
import { Card } from '../../components/Card';
import { useLocale } from '../../i18n/LocaleContext';
import { useAuth } from '../../auth/AuthContext';
import { usePanels } from '../../panels/PanelContext';
import { getEvents } from '../../api/events';
import { getUsers } from '../../api/users';
import { ResponseState, type AlarmEvent, type PanelUser } from '../../api/types';
import { Banner } from '../../components/Banner';
import { PanelToolbar } from '../../components/PanelToolbar';
import { applyEventTextSubstitutions } from '../../panels/eventText';
import type { MainStackParamList } from '../../navigation/types';

type Props = NativeStackScreenProps<MainStackParamList, 'Events'>;

// Read-only history, newest first (server already orders/limits to the last 200). Only
// StringDate + Text are ever populated by the backend -- see AlarmEvent's comment in api/types.ts.
export function EventsScreen({ navigation }: Props) {
  const { colors, typography, spacing } = useTheme();
  const { t } = useLocale();
  const { logout } = useAuth();
  const { selected } = usePanels();
  const deviceId = selected?.kind === 'ip' ? selected.deviceId : null;

  const [events, setEvents] = useState<AlarmEvent[] | null>(null);
  const [users, setUsers] = useState<PanelUser[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(async () => {
    if (!deviceId) return;
    setLoading(true);
    setError(null);
    try {
      const res = await getEvents(deviceId);
      if (res.State === ResponseState.UNAUTHORIZED) {
        // No manual navigation needed -- RootNavigator swaps to the login stack as soon as
        // session clears (see navigation/RootNavigator.tsx).
        logout();
        return;
      }
      if (res.State !== ResponseState.OK) {
        setError(res.Message || t('Could not load events.'));
        return;
      }
      setEvents(res.Events ?? []);
      // Best-effort -- current user labels are only used to re-resolve event text (see
      // applyEventTextSubstitutions); if this fails, events still show, just without labels.
      try {
        const usersRes = await getUsers(deviceId);
        setUsers(usersRes.Users ?? []);
      } catch {
        setUsers([]);
      }
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
        <Text style={[typography.bodyDim, { color: colors.onDarkDim }]}>{t('Select a Wi-Fi panel to view its events.')}</Text>
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
      ) : (events ?? []).length === 0 ? (
        <Text style={[typography.bodyDim, { color: colors.onDarkDim, textAlign: 'center', marginTop: 32 }]}>
          {t('No events yet.')}
        </Text>
      ) : (
        <View style={{ padding: 16, flex: 1 }}>
          <Card style={styles.card}>
            <FlatList
              data={events ?? []}
              keyExtractor={(e) => String(e.EventId)}
              renderItem={({ item, index }) => (
                <View
                  style={[
                    styles.row,
                    index < (events?.length ?? 0) - 1 && { borderBottomWidth: 1, borderBottomColor: colors.line },
                  ]}
                >
                  <Text style={[typography.body, { color: colors.ink, fontWeight: '700', marginBottom: spacing.xs }]}>
                    {item.StringDate}
                  </Text>
                  <Text style={[typography.bodyDim, { color: colors.inkDim }]}>
                    {applyEventTextSubstitutions(item.Text ?? '', selected?.description ?? '', users)}
                  </Text>
                </View>
              )}
            />
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
    flex: 1,
    padding: 0,
  },
  row: {
    paddingVertical: 12,
    paddingHorizontal: 16,
  },
});

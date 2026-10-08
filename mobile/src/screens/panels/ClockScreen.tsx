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
import { getTime, syncTime } from '../../api/device';
import { ResponseState, type TimeResponse } from '../../api/types';
import { PrimaryButton } from '../../components/PrimaryButton';
import { Banner } from '../../components/Banner';
import { PanelToolbar } from '../../components/PanelToolbar';
import type { MainStackParamList } from '../../navigation/types';

type Props = NativeStackScreenProps<MainStackParamList, 'Clock'>;

function formatTime(iso: string): string {
  const date = new Date(iso);
  return Number.isNaN(date.getTime()) ? iso : date.toLocaleString();
}

export function ClockScreen({ navigation }: Props) {
  const { colors, typography, spacing } = useTheme();
  const { t } = useLocale();
  const { logout } = useAuth();
  const { selected } = usePanels();
  const deviceId = selected?.kind === 'ip' ? selected.deviceId : null;

  const [time, setTime] = useState<TimeResponse | null>(null);
  const [phoneTime, setPhoneTime] = useState<Date | null>(null);
  const [loading, setLoading] = useState(true);
  const [syncing, setSyncing] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(async () => {
    if (!deviceId) return;
    setLoading(true);
    setError(null);
    try {
      const res = await getTime(deviceId);
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
        setError(res.Message || t('Could not read the panel time.'));
        return;
      }
      setTime(res);
      // Ported from the previous app: "current time" is always the phone's own clock, taken
      // client-side -- never the backend's ServerTime field. That field is the panel's *target*
      // Argentina-local time re-serialized without a timezone marker, which a phone in a
      // different Kind/zone context would then double-shift when displayed, showing a time
      // hours off from what the panel actually gets synced to.
      setPhoneTime(new Date());
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

  async function handleSync() {
    if (!deviceId) return;
    setSyncing(true);
    setError(null);
    try {
      const res = await syncTime(deviceId);
      if (res.State === ResponseState.CENTRAL_UNRESPONSIVE) {
        setError(t("The panel isn't responding right now. Try again."));
        return;
      }
      if (res.State !== ResponseState.OK) {
        setError(res.Message || t('Could not sync the panel time.'));
        return;
      }
      setTime(res);
      setPhoneTime(new Date());
    } catch {
      setError(t('Could not reach the server.'));
    } finally {
      setSyncing(false);
    }
  }

  if (!deviceId) {
    return (
      <GradientBackground style={styles.centered}>
        <Text style={[typography.bodyDim, { color: colors.onDarkDim }]}>{t('Select an IP panel to sync its clock.')}</Text>
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
          <Card>
            <Text style={[typography.body, { color: colors.ink, fontWeight: '700' }]}>{t('Panel time')}</Text>
            <Text style={[typography.body, { color: colors.inkDim, marginBottom: spacing.md }]}>
              {time ? formatTime(time.DeviceTime) : '--'}
            </Text>
            <Text style={[typography.body, { color: colors.ink, fontWeight: '700' }]}>{t('Phone time')}</Text>
            <Text style={[typography.body, { color: colors.inkDim }]}>{phoneTime ? phoneTime.toLocaleString() : '--'}</Text>
          </Card>
        </View>
      )}

      <View style={[styles.syncButton, { paddingHorizontal: 16 }]}>
        <PrimaryButton title={t('Sync panel to server time')} onPress={handleSync} loading={syncing} tone="dark" />
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
  syncButton: {
    alignSelf: 'center',
    width: '60%',
  },
});

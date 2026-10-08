import { useCallback, useEffect, useState } from 'react';
import { ActivityIndicator, FlatList, Pressable, StyleSheet, Text, View } from 'react-native';
import { useFocusEffect } from '@react-navigation/native';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import { useTheme } from '../../theme/ThemeProvider';
import { GradientBackground } from '../../components/GradientBackground';
import { useLocale } from '../../i18n/LocaleContext';
import { useAuth } from '../../auth/AuthContext';
import { usePanels } from '../../panels/PanelContext';
import { getExclusions, sendExclusions } from '../../api/zones';
import { getGeneralStatus } from '../../api/command';
import { ResponseState, type Exclusion } from '../../api/types';
import { describeCommandException, describeCommandFailure } from '../../panels/commandFeedback';
import { PrimaryButton } from '../../components/PrimaryButton';
import { Banner } from '../../components/Banner';
import { PanelToolbar } from '../../components/PanelToolbar';
import type { MainStackParamList } from '../../navigation/types';

type Props = NativeStackScreenProps<MainStackParamList, 'Exclusions'>;

const BLINK_INTERVAL_MS = 1000;

export function ExclusionsScreen({ navigation }: Props) {
  const { colors, typography, spacing, radius } = useTheme();
  const { t } = useLocale();
  const { logout } = useAuth();
  const { selected } = usePanels();
  const deviceId = selected?.kind === 'ip' ? selected.deviceId : null;

  const [exclusions, setExclusions] = useState<Exclusion[] | null>(null);
  const [initialExcluded, setInitialExcluded] = useState<Set<number>>(new Set());
  const [isArmed, setIsArmed] = useState(false);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);
  const [blinkOn, setBlinkOn] = useState(true);

  // "Armed" here means the general status doesn't include READY -- a NOT-READY panel (open,
  // unarmed zones) still contains "READY" as a substring, so it counts as not armed and bypassing
  // is allowed. Matches the previous app's exact check.
  const checkArmed = useCallback(async () => {
    if (!deviceId) return false;
    const res = await getGeneralStatus(deviceId);
    return res.Text ? !res.Text.includes('READY') : false;
  }, [deviceId]);

  const load = useCallback(async () => {
    if (!deviceId) return;
    setLoading(true);
    setError(null);
    try {
      const [armed, res] = await Promise.all([checkArmed(), getExclusions(deviceId)]);
      setIsArmed(armed);
      if (res.State === ResponseState.UNAUTHORIZED) {
        // No manual navigation needed -- RootNavigator swaps to the login stack as soon as
        // session clears (see navigation/RootNavigator.tsx).
        logout();
        return;
      }
      if (res.State !== ResponseState.OK) {
        setError(describeCommandFailure(res, t));
        return;
      }
      const list = res.Exclusions ?? [];
      setExclusions(list);
      setInitialExcluded(new Set(list.filter((e) => e.Excluded).map((e) => e.ExclusionNumber)));
    } catch (e) {
      setError(describeCommandException(e, t));
    } finally {
      setLoading(false);
    }
  }, [deviceId, checkArmed, logout, t]);

  useFocusEffect(
    useCallback(() => {
      load();
    }, [load]),
  );

  useEffect(() => {
    const interval = setInterval(() => setBlinkOn((v) => !v), BLINK_INTERVAL_MS);
    return () => clearInterval(interval);
  }, []);

  function toggle(exclusion: Exclusion) {
    if (isArmed && !exclusion.Excluded) {
      setError(t("Zones can't be excluded while the alarm is armed."));
      return;
    }
    setError(null);
    setExclusions((prev) =>
      prev
        ? prev.map((e) => (e.ExclusionNumber === exclusion.ExclusionNumber ? { ...e, Excluded: !e.Excluded } : e))
        : prev,
    );
  }

  async function handleSave() {
    if (!deviceId || !exclusions) return;
    const currentlyExcluded = exclusions.filter((e) => e.Excluded).map((e) => e.ExclusionNumber);
    const addedNew = currentlyExcluded.some((n) => !initialExcluded.has(n));

    setSaving(true);
    setError(null);
    try {
      if (addedNew) {
        // Re-validate against live status right before saving -- the alarm could have been
        // armed from another device since this screen loaded.
        const armedNow = await checkArmed();
        if (armedNow) {
          setIsArmed(true);
          setError(t("Zones can't be excluded while the alarm is armed."));
          return;
        }
      }
      const res = await sendExclusions(deviceId, currentlyExcluded);
      if (res.State === ResponseState.UNAUTHORIZED) {
        logout();
        return;
      }
      if (res.State !== ResponseState.OK || !res.Text) {
        setError(describeCommandFailure(res, t));
        return;
      }
      await load();
    } catch (e) {
      setError(describeCommandException(e, t));
    } finally {
      setSaving(false);
    }
  }

  if (!deviceId) {
    return (
      <GradientBackground style={styles.centered}>
        <Text style={[typography.bodyDim, { color: colors.onDarkDim }]}>{t('Select an IP panel to manage its exclusions.')}</Text>
      </GradientBackground>
    );
  }

  const hasChanges =
    !!exclusions &&
    JSON.stringify([...initialExcluded].sort()) !==
      JSON.stringify(exclusions.filter((e) => e.Excluded).map((e) => e.ExclusionNumber).sort());

  return (
    <GradientBackground style={styles.container}>
      <View style={{ padding: 16, paddingBottom: 0 }}>
        <PanelToolbar
          description={selected?.description ?? ''}
          onSwitchPanel={() => navigation.navigate('PanelSelector')}
          onRefresh={load}
          refreshing={loading}
        />
        {isArmed && <Banner kind="error">{t('The alarm is armed -- you can only remove existing exclusions.')}</Banner>}
        {error && <Banner kind="error">{error}</Banner>}
      </View>

      {loading ? (
        <ActivityIndicator style={{ marginTop: 32 }} size="large" color={colors.accent} />
      ) : (exclusions ?? []).length === 0 ? (
        <Text style={[typography.bodyDim, { color: colors.onDarkDim, textAlign: 'center', marginTop: 32 }]}>
          {t('No zones yet.')}
        </Text>
      ) : (
        <FlatList
          data={exclusions ?? []}
          keyExtractor={(e) => String(e.ExclusionNumber)}
          contentContainerStyle={{ padding: 16 }}
          renderItem={({ item }) => {
            const dotColor =
              item.Excluded && item.Open ? (blinkOn ? colors.warning : colors.danger) : item.Excluded ? colors.warning : item.Open ? colors.danger : colors.inkDim;
            const name = item.Name === item.ExclusionNumber.toString() ? t('Zone {n}', { n: item.ExclusionNumber }) : item.Name;

            return (
              <Pressable
                onPress={() => toggle(item)}
                style={[styles.row, { backgroundColor: colors.panel, borderColor: colors.line, borderRadius: radius.sm, marginBottom: spacing.sm }]}
              >
                <View style={[styles.dot, { backgroundColor: dotColor }]} />
                <Text style={[typography.body, { color: colors.ink, flex: 1 }]} numberOfLines={1}>
                  {name}
                </Text>
                <View
                  style={[
                    styles.checkbox,
                    {
                      borderColor: colors.line,
                      borderRadius: radius.sm,
                      backgroundColor: item.Excluded ? colors.accent : 'transparent',
                    },
                  ]}
                />
              </Pressable>
            );
          }}
        />
      )}

      <View style={{ padding: 16, paddingTop: 0 }}>
        {hasChanges && (
          <>
            <PrimaryButton title={t('Save changes')} onPress={handleSave} loading={saving} />
            <View style={{ height: spacing.sm }} />
          </>
        )}
        <PrimaryButton title={t('Back')} onPress={() => navigation.goBack()} />
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
  checkbox: {
    width: 22,
    height: 22,
    borderWidth: 1,
  },
});

import { useCallback, useState } from 'react';
import { ActivityIndicator, FlatList, Pressable, StyleSheet, Switch, Text, View } from 'react-native';
import { useFocusEffect } from '@react-navigation/native';
import { useTheme } from '../../theme/ThemeProvider';
import { GradientBackground } from '../../components/GradientBackground';
import { useLocale } from '../../i18n/LocaleContext';
import { useAuth } from '../../auth/AuthContext';
import { usePanels } from '../../panels/PanelContext';
import { getProgramControls } from '../../api/pgm';
import { createSchedule, deleteSchedule, enumSchedules, setScheduleEnabled } from '../../api/schedule';
import { ResponseState, type ProgramControl, type ScheduledPgmAction } from '../../api/types';
import { PrimaryButton } from '../../components/PrimaryButton';
import { Banner } from '../../components/Banner';
import { Card } from '../../components/Card';

// Real recurring scheduling -- "at this time, on these days, set this PGM output to this
// state" -- fired server-side by the backend's ScheduledPgmDispatcher. This used to be the same
// immediate manual PGM toggle as PgmScreen with no scheduling behind it at all.

const DAYS: { bit: number; label: string }[] = [
  { bit: 1 << 0, label: 'Sun' },
  { bit: 1 << 1, label: 'Mon' },
  { bit: 1 << 2, label: 'Tue' },
  { bit: 1 << 3, label: 'Wed' },
  { bit: 1 << 4, label: 'Thu' },
  { bit: 1 << 5, label: 'Fri' },
  { bit: 1 << 6, label: 'Sat' },
];

function formatTime(hour: number, minute: number): string {
  return `${String(hour).padStart(2, '0')}:${String(minute).padStart(2, '0')}:00`;
}

function parseTime(timeOfDay: string): { hour: number; minute: number } {
  const [h, m] = timeOfDay.split(':');
  return { hour: Number(h), minute: Number(m) };
}

export function ScheduledDeparturesScreen() {
  const { colors, typography, spacing, radius } = useTheme();
  const { t } = useLocale();
  const { logout } = useAuth();
  const { selected } = usePanels();
  const deviceId = selected?.kind === 'ip' ? selected.deviceId : null;

  const [outputs, setOutputs] = useState<ProgramControl[] | null>(null);
  const [schedules, setSchedules] = useState<ScheduledPgmAction[] | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [outputNumber, setOutputNumber] = useState(1);
  const [hour, setHour] = useState(18);
  const [minute, setMinute] = useState(0);
  const [daysMask, setDaysMask] = useState(0b0111110); // weekdays by default
  const [desiredState, setDesiredState] = useState(true);
  const [creating, setCreating] = useState(false);
  const [busyId, setBusyId] = useState<number | null>(null);

  const load = useCallback(async () => {
    if (!deviceId) return;
    setLoading(true);
    setError(null);
    try {
      const [outputsRes, schedulesRes] = await Promise.all([getProgramControls(deviceId), enumSchedules(deviceId)]);
      if (outputsRes.State === ResponseState.UNAUTHORIZED || schedulesRes.State === ResponseState.UNAUTHORIZED) {
        logout();
        return;
      }
      setOutputs(outputsRes.ProgramControls ?? []);
      setSchedules(schedulesRes.Schedules ?? []);
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

  function outputName(programControlNumber: number): string {
    const output = outputs?.find((o) => o.ProgramControlNumber === programControlNumber);
    if (!output) return `${t('Output')} ${programControlNumber}`;
    return output.Name === output.ProgramControlNumber.toString() ? `${t('Output')} ${programControlNumber}` : output.Name;
  }

  function daysSummary(mask: number): string {
    return DAYS.filter((d) => (mask & d.bit) !== 0)
      .map((d) => t(d.label))
      .join(', ');
  }

  async function handleCreate() {
    if (!deviceId || daysMask === 0) {
      if (daysMask === 0) setError(t('Choose at least one day.'));
      return;
    }
    setError(null);
    setCreating(true);
    try {
      const res = await createSchedule(deviceId, outputNumber, formatTime(hour, minute), daysMask, desiredState);
      if (res.State === ResponseState.UNAUTHORIZED) {
        logout();
        return;
      }
      if (res.State !== ResponseState.OK) {
        setError(res.Message || t('Could not save the schedule.'));
        return;
      }
      await load();
    } catch {
      setError(t('Could not reach the server.'));
    } finally {
      setCreating(false);
    }
  }

  async function handleToggleEnabled(schedule: ScheduledPgmAction, next: boolean) {
    if (!deviceId) return;
    setBusyId(schedule.ScheduledPgmActionId);
    setError(null);
    try {
      const res = await setScheduleEnabled(deviceId, schedule.ScheduledPgmActionId, next);
      if (res.State === ResponseState.UNAUTHORIZED) {
        logout();
        return;
      }
      if (res.State !== ResponseState.OK) {
        setError(res.Message || t('Could not update the schedule.'));
        return;
      }
      setSchedules((prev) =>
        prev ? prev.map((s) => (s.ScheduledPgmActionId === schedule.ScheduledPgmActionId ? { ...s, Enabled: next } : s)) : prev,
      );
    } catch {
      setError(t('Could not reach the server.'));
    } finally {
      setBusyId(null);
    }
  }

  async function handleDelete(schedule: ScheduledPgmAction) {
    if (!deviceId) return;
    setBusyId(schedule.ScheduledPgmActionId);
    setError(null);
    try {
      const res = await deleteSchedule(deviceId, schedule.ScheduledPgmActionId);
      if (res.State === ResponseState.UNAUTHORIZED) {
        logout();
        return;
      }
      if (res.State !== ResponseState.OK) {
        setError(res.Message || t('Could not delete the schedule.'));
        return;
      }
      setSchedules((prev) => (prev ? prev.filter((s) => s.ScheduledPgmActionId !== schedule.ScheduledPgmActionId) : prev));
    } catch {
      setError(t('Could not reach the server.'));
    } finally {
      setBusyId(null);
    }
  }

  if (!deviceId) {
    return (
      <GradientBackground style={styles.centered}>
        <Text style={[typography.bodyDim, { color: colors.onDarkDim }]}>{t('Select an IP panel to manage its schedules.')}</Text>
      </GradientBackground>
    );
  }

  if (loading) {
    return (
      <GradientBackground style={styles.centered}>
        <ActivityIndicator size="large" color={colors.accent} />
      </GradientBackground>
    );
  }

  return (
    <GradientBackground style={{ flex: 1 }}>
    <FlatList
      style={{ flex: 1 }}
      contentContainerStyle={{ padding: spacing.lg }}
      data={schedules ?? []}
      keyExtractor={(s) => String(s.ScheduledPgmActionId)}
      ListHeaderComponent={
        <Card style={{ marginBottom: spacing.xl }}>
          {error && <Banner kind="error">{error}</Banner>}

          <Text style={[typography.label, { color: colors.inkDim, marginBottom: spacing.sm }]}>{t('New schedule')}</Text>

          <Text style={[typography.bodyDim, { color: colors.inkDim, marginBottom: spacing.xs }]}>{t('Output')}</Text>
          <View style={[styles.chipRow, { marginBottom: spacing.md }]}>
            {(outputs ?? []).map((o) => (
              <Pressable
                key={o.ProgramControlNumber}
                onPress={() => setOutputNumber(o.ProgramControlNumber)}
                style={[
                  styles.chip,
                  {
                    borderColor: outputNumber === o.ProgramControlNumber ? colors.accent : colors.line,
                    backgroundColor: outputNumber === o.ProgramControlNumber ? colors.accent : colors.panel,
                    borderRadius: radius.pill,
                  },
                ]}
              >
                <Text
                  style={[
                    typography.bodyDim,
                    { color: outputNumber === o.ProgramControlNumber ? colors.accentInk : colors.ink },
                  ]}
                  numberOfLines={1}
                >
                  {outputName(o.ProgramControlNumber)}
                </Text>
              </Pressable>
            ))}
          </View>

          <Text style={[typography.bodyDim, { color: colors.inkDim, marginBottom: spacing.xs }]}>{t('Time')}</Text>
          <View style={[styles.timeRow, { marginBottom: spacing.md }]}>
            <Stepper value={hour} min={0} max={23} onChange={setHour} />
            <Text style={[typography.title, { color: colors.ink, marginHorizontal: spacing.sm }]}>:</Text>
            <Stepper value={minute} min={0} max={59} onChange={setMinute} pad />
          </View>

          <Text style={[typography.bodyDim, { color: colors.inkDim, marginBottom: spacing.xs }]}>{t('Repeat on')}</Text>
          <View style={[styles.chipRow, { marginBottom: spacing.md }]}>
            {DAYS.map((d) => {
              const active = (daysMask & d.bit) !== 0;
              return (
                <Pressable
                  key={d.bit}
                  onPress={() => setDaysMask((prev) => (active ? prev & ~d.bit : prev | d.bit))}
                  style={[
                    styles.dayChip,
                    {
                      borderColor: active ? colors.accent : colors.line,
                      backgroundColor: active ? colors.accent : colors.panel,
                      borderRadius: radius.pill,
                    },
                  ]}
                >
                  <Text style={[typography.bodyDim, { color: active ? colors.accentInk : colors.ink }]}>{t(d.label)}</Text>
                </Pressable>
              );
            })}
          </View>

          <Text style={[typography.bodyDim, { color: colors.inkDim, marginBottom: spacing.xs }]}>{t('Action')}</Text>
          <View style={[styles.chipRow, { marginBottom: spacing.lg }]}>
            <Pressable
              onPress={() => setDesiredState(true)}
              style={[
                styles.chip,
                {
                  borderColor: desiredState ? colors.accent : colors.line,
                  backgroundColor: desiredState ? colors.accent : colors.panel,
                  borderRadius: radius.pill,
                },
              ]}
            >
              <Text style={[typography.bodyDim, { color: desiredState ? colors.accentInk : colors.ink }]}>{t('Turn on')}</Text>
            </Pressable>
            <Pressable
              onPress={() => setDesiredState(false)}
              style={[
                styles.chip,
                {
                  borderColor: !desiredState ? colors.accent : colors.line,
                  backgroundColor: !desiredState ? colors.accent : colors.panel,
                  borderRadius: radius.pill,
                },
              ]}
            >
              <Text style={[typography.bodyDim, { color: !desiredState ? colors.accentInk : colors.ink }]}>{t('Turn off')}</Text>
            </Pressable>
          </View>

          <PrimaryButton title={t('Add schedule')} onPress={handleCreate} loading={creating} />

          <Text style={[typography.label, { color: colors.inkDim, marginTop: spacing.xl, marginBottom: spacing.sm }]}>
            {t('Scheduled actions')}
          </Text>
          {(schedules ?? []).length === 0 && (
            <Text style={[typography.bodyDim, { color: colors.inkDim }]}>{t('No schedules yet.')}</Text>
          )}
        </Card>
      }
      renderItem={({ item }) => (
        <View style={[styles.scheduleRow, { backgroundColor: colors.panel, borderColor: colors.line, borderRadius: radius.sm, marginBottom: spacing.sm }]}>
          <View style={{ flex: 1 }}>
            <Text style={[typography.body, { color: colors.ink }]}>
              {outputName(item.ProgramControlNumber)} &middot; {item.DesiredState ? t('On') : t('Off')}
            </Text>
            <Text style={[typography.bodyDim, { color: colors.inkDim, marginTop: 2 }]}>
              {(() => {
                const { hour: h, minute: m } = parseTime(item.TimeOfDay);
                return `${String(h).padStart(2, '0')}:${String(m).padStart(2, '0')}`;
              })()}{' '}
              &middot; {daysSummary(item.DaysOfWeekMask)}
            </Text>
          </View>
          {busyId === item.ScheduledPgmActionId ? (
            <ActivityIndicator color={colors.accent} />
          ) : (
            <>
              <Switch
                value={item.Enabled}
                onValueChange={(next) => handleToggleEnabled(item, next)}
                trackColor={{ true: colors.accent, false: colors.line }}
              />
              <Pressable onPress={() => handleDelete(item)} hitSlop={8} style={{ marginLeft: spacing.md }}>
                <Text style={[typography.bodyDim, { color: colors.danger }]}>{t('Delete')}</Text>
              </Pressable>
            </>
          )}
        </View>
      )}
    />
    </GradientBackground>
  );
}

function Stepper({
  value,
  min,
  max,
  step = 1,
  onChange,
  pad,
}: {
  value: number;
  min: number;
  max: number;
  step?: number;
  onChange: (v: number) => void;
  pad?: boolean;
}) {
  const { colors, typography, radius } = useTheme();
  function wrap(next: number): number {
    if (next > max) return min;
    if (next < min) return max;
    return next;
  }
  return (
    <View style={[styles.stepper, { borderColor: colors.line, borderRadius: radius.sm }]}>
      <Pressable onPress={() => onChange(wrap(value - step))} hitSlop={8} style={styles.stepperBtn}>
        <Text style={[typography.title, { color: colors.accent }]}>&minus;</Text>
      </Pressable>
      <Text style={[typography.title, { color: colors.ink, minWidth: 32, textAlign: 'center' }]}>
        {pad ? String(value).padStart(2, '0') : value}
      </Text>
      <Pressable onPress={() => onChange(wrap(value + step))} hitSlop={8} style={styles.stepperBtn}>
        <Text style={[typography.title, { color: colors.accent }]}>+</Text>
      </Pressable>
    </View>
  );
}

const styles = StyleSheet.create({
  centered: { flex: 1, alignItems: 'center', justifyContent: 'center', padding: 24 },
  chipRow: { flexDirection: 'row', flexWrap: 'wrap', gap: 8 },
  chip: { borderWidth: 1, paddingVertical: 8, paddingHorizontal: 14 },
  dayChip: { borderWidth: 1, paddingVertical: 8, paddingHorizontal: 10 },
  timeRow: { flexDirection: 'row', alignItems: 'center' },
  stepper: { flexDirection: 'row', alignItems: 'center', borderWidth: 1, paddingHorizontal: 4 },
  stepperBtn: { paddingHorizontal: 10, paddingVertical: 6 },
  scheduleRow: { flexDirection: 'row', alignItems: 'center', borderWidth: 1, padding: 12 },
});

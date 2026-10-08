import { useCallback, useState } from 'react';
import { ActivityIndicator, FlatList, Modal, Pressable, StyleSheet, Switch, Text, View } from 'react-native';
import { useFocusEffect, useNavigation } from '@react-navigation/native';
import { useTheme } from '../../theme/ThemeProvider';
import { GradientBackground } from '../../components/GradientBackground';
import { Card } from '../../components/Card';
import { useLocale } from '../../i18n/LocaleContext';
import { useAuth } from '../../auth/AuthContext';
import { usePanels } from '../../panels/PanelContext';
import { getProgramControls, saveProgramControlNames, toggleProgramControl } from '../../api/pgm';
import { ResponseState, type ProgramControl } from '../../api/types';
import { describeCommandException, describeCommandFailure } from '../../panels/commandFeedback';
import { PrimaryButton } from '../../components/PrimaryButton';
import { Banner } from '../../components/Banner';
import { FormField } from '../../components/FormField';
import { LoadingOverlay } from '../../components/LoadingOverlay';
import { PanelToolbar } from '../../components/PanelToolbar';

interface ProgramControlListProps {
  // The panel has one physical concept (8 PGM outputs) that this app surfaces under two names --
  // "PGM outputs" for general use, "Scheduled departures" for the departure-trigger use case the
  // old app split into its own screen. Same API, same 8 slots, just different framing/copy.
  labelPrefix: string;
  emptyNamePlaceholder: string;
}

// New APP Videos/'s "SALIDAS PROGRAMABLES" screen (Failures_NoFailures_PGMoutputs.mp4) renames a
// row through a "Cambiar nombre" modal dialog that saves immediately (Aguarde -> "Cambio guardado
// correctamente"), not this screen's previous inline-edit-then-batch-"Save changes" flow -- that
// batching (and its beforeRemove "unsaved changes" prompt) doesn't exist in the reference at all.
export function ProgramControlList({ labelPrefix, emptyNamePlaceholder }: ProgramControlListProps) {
  const { colors, typography, spacing } = useTheme();
  const { t } = useLocale();
  const { logout } = useAuth();
  const { selected } = usePanels();
  const deviceId = selected?.kind === 'ip' ? selected.deviceId : null;
  const navigation = useNavigation();

  const [outputs, setOutputs] = useState<ProgramControl[] | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [editingPgm, setEditingPgm] = useState<number | null>(null);
  const [editValue, setEditValue] = useState('');
  const [saving, setSaving] = useState(false);
  const [togglingPgm, setTogglingPgm] = useState<number | null>(null);

  const load = useCallback(async () => {
    if (!deviceId) return;
    setLoading(true);
    setError(null);
    try {
      const res = await getProgramControls(deviceId);
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
      setOutputs(res.ProgramControls ?? []);
    } catch (e) {
      setError(describeCommandException(e, t));
    } finally {
      setLoading(false);
    }
  }, [deviceId, logout, t]);

  useFocusEffect(
    useCallback(() => {
      load();
    }, [load]),
  );

  async function handleToggle(output: ProgramControl, next: boolean) {
    if (!deviceId) return;
    setError(null);
    setTogglingPgm(output.ProgramControlNumber);
    try {
      const res = await toggleProgramControl(deviceId, output.ProgramControlNumber, next);
      if (res.State === ResponseState.UNAUTHORIZED) {
        logout();
        return;
      }
      if (res.State !== ResponseState.OK) {
        setError(describeCommandFailure(res, t));
        return;
      }
      const updated = (res.ProgramControls ?? [])[0];
      if (updated) {
        setOutputs((prev) =>
          prev ? prev.map((o) => (o.ProgramControlNumber === updated.ProgramControlNumber ? updated : o)) : prev,
        );
      }
    } catch (e) {
      setError(describeCommandException(e, t));
    } finally {
      setTogglingPgm(null);
    }
  }

  function startEdit(output: ProgramControl) {
    setSuccess(null);
    setEditingPgm(output.ProgramControlNumber);
    setEditValue(output.Name === output.ProgramControlNumber.toString() ? '' : output.Name);
  }

  async function confirmEdit() {
    if (editingPgm === null || !deviceId) return;
    const trimmed = editValue.trim();
    if (!trimmed) {
      setEditingPgm(null);
      return;
    }
    const pgmNumber = editingPgm;
    setSaving(true);
    setError(null);
    try {
      const res = await saveProgramControlNames(deviceId, [{ ProgramControlNumber: pgmNumber, Name: trimmed }]);
      if (res.State === ResponseState.UNAUTHORIZED) {
        logout();
        return;
      }
      if (res.State !== ResponseState.OK) {
        setError(describeCommandFailure(res, t));
        return;
      }
      setEditingPgm(null);
      setSuccess(t('Change saved successfully'));
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
        <Text style={[typography.bodyDim, { color: colors.onDarkDim }]}>
          {t('Select an IP panel to manage its {name}.', { name: emptyNamePlaceholder.toLowerCase() })}
        </Text>
      </GradientBackground>
    );
  }

  return (
    <>
    <GradientBackground style={{ flex: 1 }}>
      <View style={{ padding: 16, paddingBottom: 0 }}>
        <PanelToolbar
          description={selected?.description ?? ''}
          onSwitchPanel={() => (navigation as any).navigate('PanelSelector')}
          onRefresh={load}
          refreshing={loading}
        />
      </View>

      {error && (
        <View style={{ padding: 16, paddingBottom: 0 }}>
          <Banner kind="error">{error}</Banner>
        </View>
      )}
      {success && (
        <View style={{ padding: 16, paddingBottom: 0 }}>
          <Banner kind="ok">{success}</Banner>
        </View>
      )}

      {loading ? (
        <ActivityIndicator style={{ marginTop: 32 }} size="large" color={colors.accent} />
      ) : (outputs ?? []).length === 0 ? (
        <Text style={[typography.bodyDim, { color: colors.onDarkDim, textAlign: 'center', marginTop: 32 }]}>
          {t('No {name} yet.', { name: emptyNamePlaceholder.toLowerCase() })}
        </Text>
      ) : (
        <View style={{ padding: 16, flex: 1 }}>
          <Card style={styles.card}>
            <FlatList
              data={outputs ?? []}
              keyExtractor={(o) => String(o.ProgramControlNumber)}
              renderItem={({ item, index }) => {
                const displayName =
                  item.Name === item.ProgramControlNumber.toString()
                    ? `${labelPrefix} ${item.ProgramControlNumber}`
                    : item.Name; // labelPrefix is already localized by the caller (PgmScreen/ScheduledDeparturesScreen)
                return (
                  <View
                    style={[
                      styles.row,
                      index < (outputs?.length ?? 0) - 1 && { borderBottomWidth: 1, borderBottomColor: colors.line },
                    ]}
                  >
                    {togglingPgm === item.ProgramControlNumber ? (
                      <ActivityIndicator color={colors.accent} />
                    ) : (
                      <Switch
                        value={item.Activated}
                        onValueChange={(next) => handleToggle(item, next)}
                        trackColor={{ true: colors.accent, false: colors.line }}
                      />
                    )}
                    <Text style={[typography.body, { color: colors.ink, flex: 1, marginLeft: spacing.md }]} numberOfLines={1}>
                      {displayName}
                    </Text>
                    <Pressable onPress={() => startEdit(item)} hitSlop={8}>
                      <Text style={{ fontSize: 18 }}>{'✏️'}</Text>
                    </Pressable>
                  </View>
                );
              }}
            />
          </Card>
        </View>
      )}
    </GradientBackground>

    <Modal visible={editingPgm !== null} transparent animationType="fade" onRequestClose={() => setEditingPgm(null)}>
      <View style={styles.modalBackdrop}>
        <Card style={styles.modalCard}>
          <Text style={[typography.title, { color: colors.ink, marginBottom: spacing.md, textAlign: 'center' }]}>
            {t('Change name')}
          </Text>
          <FormField label={labelPrefix} value={editValue} onChangeText={setEditValue} autoFocus />
          <View style={styles.modalButtonRow}>
            <View style={{ flex: 1, marginRight: spacing.sm }}>
              <PrimaryButton title={t('Cancel')} onPress={() => setEditingPgm(null)} tone="outline" disabled={saving} />
            </View>
            <View style={{ flex: 1 }}>
              <PrimaryButton title={t('Ok')} onPress={confirmEdit} loading={saving} />
            </View>
          </View>
        </Card>
      </View>
    </Modal>
    <LoadingOverlay visible={saving} label={t('Please wait')} />
    </>
  );
}

const styles = StyleSheet.create({
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
    flexDirection: 'row',
    alignItems: 'center',
    paddingVertical: 12,
    paddingHorizontal: 16,
  },
  modalBackdrop: {
    flex: 1,
    alignItems: 'center',
    justifyContent: 'center',
    backgroundColor: 'rgba(0,0,0,0.35)',
    padding: 24,
  },
  modalCard: {
    width: '100%',
    maxWidth: 360,
  },
  modalButtonRow: {
    flexDirection: 'row',
    marginTop: 8,
  },
});

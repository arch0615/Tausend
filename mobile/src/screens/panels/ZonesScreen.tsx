import { useCallback, useEffect, useState } from 'react';
import { ActivityIndicator, Alert, FlatList, KeyboardAvoidingView, Modal, Platform, Pressable, StyleSheet, Text, View } from 'react-native';
import { useFocusEffect } from '@react-navigation/native';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import { useTheme } from '../../theme/ThemeProvider';
import { GradientBackground } from '../../components/GradientBackground';
import { Card } from '../../components/Card';
import { FormField } from '../../components/FormField';
import { useLocale } from '../../i18n/LocaleContext';
import { useAuth } from '../../auth/AuthContext';
import { usePanels } from '../../panels/PanelContext';
import { getZones, saveZoneNames } from '../../api/zones';
import { ResponseState, type Zone } from '../../api/types';
import { describeCommandException, describeCommandFailure } from '../../panels/commandFeedback';
import { PrimaryButton } from '../../components/PrimaryButton';
import { Banner } from '../../components/Banner';
import { PanelToolbar } from '../../components/PanelToolbar';
import type { MainStackParamList } from '../../navigation/types';

type Props = NativeStackScreenProps<MainStackParamList, 'Zones'>;

const BLINK_INTERVAL_MS = 1000;

// New APP Videos/'s "ZONAS" screen (Zones edition-exclusion-memory.mp4) renames a zone through a
// "Cambiar nombre" modal (a single field prefilled with the zone's current name, Cancelar/Ok),
// not this screen's previous inline-edit-then-"Done"-link -- ported from zones.page.ts's onClick(),
// which shows exactly that alert. The batched "Guardar Cambios" save (changeNames() only fires on
// that button, same as zones.page.ts) is unchanged -- unlike PGM outputs, this legacy screen never
// saved renames immediately either.
export function ZonesScreen({ navigation }: Props) {
  const { colors, typography, spacing } = useTheme();
  const { t } = useLocale();
  const { logout } = useAuth();
  const { selected } = usePanels();
  const deviceId = selected?.kind === 'ip' ? selected.deviceId : null;

  const [zones, setZones] = useState<Zone[] | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [pendingChanges, setPendingChanges] = useState<Record<number, string>>({});
  const [editingZone, setEditingZone] = useState<Zone | null>(null);
  const [editValue, setEditValue] = useState('');
  const [saving, setSaving] = useState(false);
  const [blinkOn, setBlinkOn] = useState(true);

  const hasPendingChanges = Object.keys(pendingChanges).length > 0;

  const load = useCallback(async () => {
    if (!deviceId) return;
    setLoading(true);
    setError(null);
    try {
      const res = await getZones(deviceId);
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
      setZones(res.Zones ?? []);
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

  // Blinking dot for zones that are both excluded and currently open.
  useEffect(() => {
    const interval = setInterval(() => setBlinkOn((v) => !v), BLINK_INTERVAL_MS);
    return () => clearInterval(interval);
  }, []);

  // Matches the previous app's unsaved-changes guard on leaving the screen.
  useEffect(() => {
    return navigation.addListener('beforeRemove', (e) => {
      if (!hasPendingChanges) return;
      e.preventDefault();
      Alert.alert(t('Unsaved changes'), t('You have unsaved zone names. Leave without saving?'), [
        { text: t('Stay'), style: 'cancel' },
        { text: t('Discard'), style: 'destructive', onPress: () => navigation.dispatch(e.data.action) },
      ]);
    });
  }, [navigation, hasPendingChanges, t]);

  function startEdit(zone: Zone) {
    setEditingZone(zone);
    setEditValue(pendingChanges[zone.ZoneNumber] ?? (zone.Name === zone.ZoneNumber.toString() ? '' : zone.Name));
  }

  function confirmEdit() {
    if (!editingZone) return;
    const trimmed = editValue.trim();
    if (trimmed) {
      setPendingChanges((prev) => ({ ...prev, [editingZone.ZoneNumber]: trimmed }));
    }
    setEditingZone(null);
  }

  async function handleSave() {
    if (!deviceId || !hasPendingChanges) return;
    setSaving(true);
    setError(null);
    try {
      const changes = Object.entries(pendingChanges).map(([zoneNumber, name]) => ({
        ZoneNumber: Number(zoneNumber),
        Name: name,
      }));
      const res = await saveZoneNames(deviceId, changes);
      if (res.State === ResponseState.UNAUTHORIZED) {
        logout();
        return;
      }
      if (res.State !== ResponseState.OK) {
        setError(describeCommandFailure(res, t));
        return;
      }
      setPendingChanges({});
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
        <Text style={[typography.bodyDim, { color: colors.onDarkDim }]}>{t('Select a Wi-Fi panel to manage its zones.')}</Text>
      </GradientBackground>
    );
  }

  return (
    <>
    <GradientBackground style={{ flex: 1 }}>
      <View style={{ padding: 16, paddingBottom: 0 }}>
        <PanelToolbar
          description={selected?.description ?? ''}
          onSwitchPanel={() => navigation.navigate('PanelSelector')}
          onRefresh={load}
          refreshing={loading}
        />
        <View style={styles.topRow}>
          <View style={{ flex: 1, marginRight: spacing.sm }}>
            <PrimaryButton title={t('Exclusions')} onPress={() => navigation.navigate('Exclusions')} tone="dark" />
          </View>
          <View style={{ flex: 1 }}>
            <PrimaryButton title={t('Memory')} onPress={() => navigation.navigate('Memory')} tone="dark" />
          </View>
        </View>
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
          {t('No zones yet.')}
        </Text>
      ) : (
        <FlatList
          data={zones ?? []}
          keyExtractor={(z) => String(z.ZoneNumber)}
          contentContainerStyle={{ padding: 16 }}
          renderItem={({ item }) => {
            const pendingName = pendingChanges[item.ZoneNumber];
            const displayName = pendingName ?? (item.Name === item.ZoneNumber.toString() ? t('Zone {n}', { n: item.ZoneNumber }) : item.Name);
            const dotColor = zoneDotColor(item, blinkOn, colors);

            return (
              <View style={[styles.row, { marginBottom: spacing.md }]}>
                <View style={[styles.circle, { backgroundColor: dotColor }]}>
                  <Text style={styles.circleIcon}>{'◫'}</Text>
                </View>
                <Text style={[typography.body, { color: colors.onDark, flex: 1, marginLeft: spacing.md }]} numberOfLines={1}>
                  {displayName}
                  {pendingName ? ' *' : ''}
                </Text>
                <Pressable onPress={() => startEdit(item)} hitSlop={8}>
                  <Text style={{ fontSize: 18 }}>{'✏️'}</Text>
                </Pressable>
              </View>
            );
          }}
        />
      )}

      <View style={{ padding: 16, paddingTop: 0 }}>
        {hasPendingChanges ? (
          <View style={styles.bottomRow}>
            <View style={{ flex: 1, marginRight: spacing.sm }}>
              <PrimaryButton title={t('Save changes')} onPress={handleSave} loading={saving} tone="dark" />
            </View>
            <View style={{ flex: 1 }}>
              <PrimaryButton title={t('Back')} onPress={() => navigation.goBack()} tone="outline" />
            </View>
          </View>
        ) : (
          <PrimaryButton title={t('Back')} onPress={() => navigation.goBack()} tone="outline" />
        )}
      </View>
    </GradientBackground>

    <Modal visible={editingZone !== null} transparent animationType="fade" onRequestClose={() => setEditingZone(null)}>
      <KeyboardAvoidingView style={styles.modalBackdrop} behavior={Platform.OS === 'ios' ? 'padding' : 'height'}>
        <Card style={styles.modalCard}>
          <Text style={[typography.title, { color: colors.ink, marginBottom: spacing.md, textAlign: 'center' }]}>
            {t('Change name')}
          </Text>
          <FormField label={t('Name')} value={editValue} onChangeText={setEditValue} autoFocus />
          <View style={styles.modalButtonRow}>
            <View style={{ flex: 1, marginRight: spacing.sm }}>
              <PrimaryButton title={t('Cancel')} onPress={() => setEditingZone(null)} tone="outline" />
            </View>
            <View style={{ flex: 1 }}>
              <PrimaryButton title={t('Ok')} onPress={confirmEdit} />
            </View>
          </View>
        </Card>
      </KeyboardAvoidingView>
    </Modal>
    </>
  );
}

function zoneDotColor(zone: Zone, blinkOn: boolean, colors: ReturnType<typeof useTheme>['colors']): string {
  if (zone.Excluded && zone.Open) return blinkOn ? colors.warning : colors.danger;
  if (zone.Excluded) return colors.warning;
  if (zone.Open) return colors.danger;
  return colors.inkDim;
}

const styles = StyleSheet.create({
  centered: {
    flex: 1,
    alignItems: 'center',
    justifyContent: 'center',
    padding: 24,
  },
  row: {
    flexDirection: 'row',
    alignItems: 'center',
  },
  circle: {
    width: 36,
    height: 36,
    borderRadius: 18,
    alignItems: 'center',
    justifyContent: 'center',
  },
  circleIcon: {
    fontSize: 16,
    color: '#ffffff',
  },
  topRow: {
    flexDirection: 'row',
    marginTop: 12,
  },
  bottomRow: {
    flexDirection: 'row',
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

import { useCallback, useEffect, useMemo, useState } from 'react';
import { ActivityIndicator, Alert, FlatList, KeyboardAvoidingView, Modal, Platform, Pressable, StyleSheet, Text, View } from 'react-native';
import { useFocusEffect, useNavigation } from '@react-navigation/native';
import { useTheme } from '../../theme/ThemeProvider';
import { GradientBackground } from '../../components/GradientBackground';
import { Card } from '../../components/Card';
import { FormField } from '../../components/FormField';
import { useLocale } from '../../i18n/LocaleContext';
import { useAuth } from '../../auth/AuthContext';
import { usePanels } from '../../panels/PanelContext';
import { getUsers, saveUsers } from '../../api/users';
import { ResponseState, type PanelUser } from '../../api/types';
import { describeCommandException, describeCommandFailure } from '../../panels/commandFeedback';
import { PrimaryButton } from '../../components/PrimaryButton';
import { Banner } from '../../components/Banner';
import { PanelToolbar } from '../../components/PanelToolbar';

interface Row {
  userNumber: number;
  name: string;
}

// Purely cosmetic labels for PIN-holders (e.g. "Mom" for user 3) -- the panel is never told these,
// it only knows PIN codes. User numbers are freely chosen (not a fixed 1..32/1..8 range like
// Zones/PGM), and there's no true delete: "removing" a label stages an empty name, matching the
// backend's upsert-only CreateUsers endpoint. Ported from users-labels.page.ts: every add/edit/
// delete is staged locally and only reaches the panel on "Guardar Cambios" -- unlike PGM outputs'
// screen (ProgramControlList.tsx), which saves each rename immediately. Different legacy screens,
// different save models; this one is deliberately still batched.
export function PanelUsersScreen() {
  const { colors, typography, spacing } = useTheme();
  const { t } = useLocale();
  const { logout } = useAuth();
  const { selected } = usePanels();
  const deviceId = selected?.kind === 'ip' ? selected.deviceId : null;
  const navigation = useNavigation();

  const [users, setUsers] = useState<PanelUser[] | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [pendingChanges, setPendingChanges] = useState<Record<number, string>>({});
  const [removed, setRemoved] = useState<Set<number>>(new Set());
  const [saving, setSaving] = useState(false);

  const [modalMode, setModalMode] = useState<'add' | 'edit' | null>(null);
  const [modalNumber, setModalNumber] = useState('');
  const [modalName, setModalName] = useState('');
  const [modalError, setModalError] = useState<string | null>(null);

  const hasPendingChanges = Object.keys(pendingChanges).length > 0 || removed.size > 0;

  const load = useCallback(async () => {
    if (!deviceId) return;
    setLoading(true);
    setError(null);
    try {
      const res = await getUsers(deviceId);
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
      setUsers(res.Users ?? []);
      setPendingChanges({});
      setRemoved(new Set());
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

  useEffect(() => {
    return navigation.addListener('beforeRemove', (e) => {
      if (!hasPendingChanges) return;
      e.preventDefault();
      Alert.alert(t('Unsaved changes'), t('You have unsaved user labels. Leave without saving?'), [
        { text: t('Stay'), style: 'cancel' },
        { text: t('Discard'), style: 'destructive', onPress: () => navigation.dispatch(e.data.action) },
      ]);
    });
  }, [navigation, hasPendingChanges, t]);

  const rows: Row[] = useMemo(() => {
    const byNumber = new Map<number, string>();
    (users ?? []).forEach((u) => byNumber.set(u.UserNumber, u.UserName));
    Object.entries(pendingChanges).forEach(([num, name]) => byNumber.set(Number(num), name));
    return [...byNumber.entries()]
      // An empty name is the server's own "removed" signal (there's no delete endpoint, see
      // api/users.ts) -- without this, a label removed in an earlier session reappears as a
      // blank row every time this screen loads fresh, since `removed` only tracks same-session
      // taps on "Remove" below.
      .filter(([num, name]) => !removed.has(num) && name !== '')
      .map(([userNumber, name]) => ({ userNumber, name }))
      .sort((a, b) => a.userNumber - b.userNumber);
  }, [users, pendingChanges, removed]);

  function openAddModal() {
    setModalMode('add');
    setModalNumber('');
    setModalName('');
    setModalError(null);
  }

  function openEditModal(row: Row) {
    setModalMode('edit');
    setModalNumber(String(row.userNumber));
    setModalName(row.name);
    setModalError(null);
  }

  function handleRemove(row: Row) {
    setRemoved((prev) => new Set(prev).add(row.userNumber));
    setPendingChanges((prev) => ({ ...prev, [row.userNumber]: '' }));
  }

  // Matches the reference's row action -- a "Seleccionar acción" choice between deleting or
  // editing the label, not separate always-visible Edit/Remove buttons.
  function openRowActions(row: Row) {
    Alert.alert(t('Select action'), undefined, [
      { text: t('Cancel'), style: 'cancel' },
      { text: t('Delete label'), style: 'destructive', onPress: () => handleRemove(row) },
      { text: t('Edit label'), onPress: () => openEditModal(row) },
    ]);
  }

  function confirmModal() {
    const num = Number(modalNumber.trim());
    const name = modalName.trim();
    if (!Number.isInteger(num) || num <= 0) {
      setModalError(t('Enter a valid PIN-holder number.'));
      return;
    }
    if (!name) {
      setModalError(t('Enter a label for this PIN-holder.'));
      return;
    }
    if (modalMode === 'add' && rows.some((r) => r.userNumber === num)) {
      setModalError(t('That PIN-holder number already has a label.'));
      return;
    }
    setPendingChanges((prev) => ({ ...prev, [num]: name }));
    setRemoved((prev) => {
      const next = new Set(prev);
      next.delete(num);
      return next;
    });
    setModalMode(null);
  }

  async function handleSave() {
    if (!deviceId || !hasPendingChanges) return;
    setSaving(true);
    setError(null);
    try {
      const changes = Object.entries(pendingChanges).map(([userNumber, name]) => ({
        UserNumber: Number(userNumber),
        UserName: name,
      }));
      const res = await saveUsers(deviceId, changes);
      if (res.State === ResponseState.UNAUTHORIZED) {
        logout();
        return;
      }
      if (res.State !== ResponseState.OK) {
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
        <Text style={[typography.bodyDim, { color: colors.onDarkDim }]}>{t('Select a Wi-Fi panel to manage its user labels.')}</Text>
      </GradientBackground>
    );
  }

  return (
    <>
    <GradientBackground style={{ flex: 1 }}>
      <View style={{ padding: 16, paddingBottom: 0 }}>
        <PanelToolbar description={selected?.description ?? ''} onSwitchPanel={() => navigation.navigate('PanelSelector' as never)} />
      </View>

      {error && (
        <View style={{ padding: 16, paddingBottom: 0 }}>
          <Banner kind="error">{error}</Banner>
        </View>
      )}

      <View style={{ padding: 16, paddingBottom: 0 }}>
        <PrimaryButton title={t('Add label')} onPress={openAddModal} tone="dark" />
      </View>

      {loading ? (
        <ActivityIndicator style={{ marginTop: 32 }} size="large" color={colors.accent} />
      ) : rows.length === 0 ? (
        <Text style={[typography.bodyDim, { color: colors.onDarkDim, textAlign: 'center', marginTop: 32 }]}>
          {t('No PIN-holder labels yet.')}
        </Text>
      ) : (
        <View style={{ padding: 16, flex: 1 }}>
          <Card style={styles.card}>
            <FlatList
              data={rows}
              keyExtractor={(r) => String(r.userNumber)}
              renderItem={({ item, index }) => (
                <Pressable
                  onPress={() => openRowActions(item)}
                  style={[styles.row, index < rows.length - 1 && { borderBottomWidth: 1, borderBottomColor: colors.line }]}
                >
                  <View style={[styles.avatar, { backgroundColor: colors.accent }]}>
                    <Text style={styles.avatarIcon}>{'👤'}</Text>
                  </View>
                  <View style={{ flex: 1, marginLeft: spacing.md }}>
                    <Text style={[typography.body, { color: colors.ink, fontWeight: '700' }]} numberOfLines={1}>
                      {item.name}
                    </Text>
                    <Text style={[typography.bodyDim, { color: colors.inkDim, marginTop: 2 }]}>
                      {t('User number {n}', { n: item.userNumber })}
                    </Text>
                  </View>
                  <Text style={{ fontSize: 18, color: colors.inkDim }}>{'⋮'}</Text>
                </Pressable>
              )}
            />
          </Card>
        </View>
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

    <Modal visible={modalMode !== null} transparent animationType="fade" onRequestClose={() => setModalMode(null)}>
      <KeyboardAvoidingView style={styles.modalBackdrop} behavior={Platform.OS === 'ios' ? 'padding' : 'height'}>
        <Card style={styles.modalCard}>
          <Text style={[typography.title, { color: colors.ink, marginBottom: spacing.md, textAlign: 'center' }]}>
            {modalMode === 'edit' ? t('Edit user label') : t('Add label')}
          </Text>
          {modalError && <Banner kind="error">{modalError}</Banner>}
          <FormField
            label={t('PIN-holder number')}
            value={modalNumber}
            onChangeText={setModalNumber}
            keyboardType="number-pad"
            editable={modalMode === 'add'}
          />
          <FormField label={t('Label')} value={modalName} onChangeText={setModalName} />
          <View style={styles.modalButtonRow}>
            <View style={{ flex: 1, marginRight: spacing.sm }}>
              <PrimaryButton title={t('Cancel')} onPress={() => setModalMode(null)} tone="outline" />
            </View>
            <View style={{ flex: 1 }}>
              <PrimaryButton title={t('Confirm')} onPress={confirmModal} />
            </View>
          </View>
        </Card>
      </KeyboardAvoidingView>
    </Modal>
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
  avatar: {
    width: 36,
    height: 36,
    borderRadius: 18,
    alignItems: 'center',
    justifyContent: 'center',
  },
  avatarIcon: {
    fontSize: 16,
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

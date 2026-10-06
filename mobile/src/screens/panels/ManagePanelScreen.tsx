import { useState } from 'react';
import { Alert, KeyboardAvoidingView, Platform, ScrollView, StyleSheet, Text, View } from 'react-native';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import { useTheme } from '../../theme/ThemeProvider';
import { GradientBackground } from '../../components/GradientBackground';
import { Card } from '../../components/Card';
import { FormField } from '../../components/FormField';
import { PanelToolbar } from '../../components/PanelToolbar';
import { useLocale } from '../../i18n/LocaleContext';
import { useAuth } from '../../auth/AuthContext';
import { usePanels } from '../../panels/PanelContext';
import { blockPin, deleteDeviceSms, disassociateCentral, updateDevice, updateDeviceSms } from '../../api/device';
import { ResponseState } from '../../api/types';
import { describeCommandException, describeCommandFailure } from '../../panels/commandFeedback';
import { PrimaryButton } from '../../components/PrimaryButton';
import { Banner } from '../../components/Banner';
import type { MainStackParamList } from '../../navigation/types';

type Props = NativeStackScreenProps<MainStackParamList, 'ManagePanel'>;

export function ManagePanelScreen({ route, navigation }: Props) {
  const { colors, typography, spacing } = useTheme();
  const { t } = useLocale();
  const { panels } = usePanels();
  const { logout, session, refreshSession } = useAuth();
  const [error, setError] = useState<string | null>(null);
  const [working, setWorking] = useState(false);

  const panel = panels.find((p) => p.kind === route.params.kind && p.deviceId === route.params.deviceId);

  // The previous app's "Vincular Equipo" edit screen showed and let you edit Descripción,
  // Identificador and Código de Usuario -- client's "Vincular Equipo" feedback: this screen (its
  // closest equivalent for an already-linked panel) showed neither. The backend's UpdateDevice/
  // UpdateDeviceSMS endpoints and mobile's updateDevice/updateDeviceSms wrappers already existed
  // fully built, just never wired to any screen.
  const ipDevice = panel?.kind === 'ip' ? session?.account.Devices.find((d) => d.DeviceId === panel.deviceId) : undefined;
  const smsDevice = panel?.kind === 'sms' ? session?.account.SmsDevices.find((d) => d.DeviceId === panel.deviceId) : undefined;

  const savedDescription = panel?.description ?? '';
  const savedIdentifier = panel?.identifier ?? '';
  const savedPin = ipDevice?.Pin ?? smsDevice?.DevicePin ?? '';

  const [description, setDescription] = useState(savedDescription);
  const [identifier, setIdentifier] = useState(savedIdentifier);
  const [pin, setPin] = useState(savedPin);
  const [updating, setUpdating] = useState(false);

  if (!panel) {
    return (
      <GradientBackground style={styles.centered}>
        <Text style={[typography.bodyDim, { color: colors.onDarkDim }]}>{t('This panel is no longer linked to your account.')}</Text>
      </GradientBackground>
    );
  }

  function resetFields() {
    setDescription(savedDescription);
    setIdentifier(savedIdentifier);
    setPin(savedPin);
  }

  async function handleUpdate() {
    if (!panel) return;
    setError(null);
    setUpdating(true);
    try {
      const res =
        panel.kind === 'ip'
          ? await updateDevice(panel.deviceId, description.trim(), identifier.trim(), pin.trim())
          : await updateDeviceSms(
              panel.deviceId,
              description.trim(),
              identifier.trim(),
              pin.trim(),
              smsDevice?.SimPin ?? '',
              smsDevice?.PhoneNumber ?? '',
              smsDevice?.DeviceType ?? '',
            );
      if (res.State === ResponseState.UNAUTHORIZED) {
        logout();
        return;
      }
      if (res.State !== ResponseState.OK) {
        setError(describeCommandFailure(res, t));
        return;
      }
      // So the new values show up immediately here and in the panel selector, without waiting
      // for the next full app launch (session.account.Devices is otherwise only refreshed then).
      await refreshSession();
    } catch (e) {
      setError(describeCommandException(e, t));
    } finally {
      setUpdating(false);
    }
  }

  function confirmUnlink() {
    if (!panel) return;
    const message =
      panel.kind === 'ip'
        ? t(
            "This removes the panel from your account only -- it keeps working normally for anyone else who has it linked, and they won't need to log in again. You'll be logged out afterward.",
          )
        : t("This permanently removes this SMS panel from your account. You'll be logged out afterward.");
    Alert.alert(t('Unlink this panel?'), message, [
      { text: t('Cancel'), style: 'cancel' },
      { text: t('Unlink'), style: 'destructive', onPress: handleUnlink },
    ]);
  }

  async function handleUnlink() {
    if (!panel) return;
    setError(null);
    setWorking(true);
    try {
      const res =
        panel.kind === 'ip' ? await disassociateCentral(panel.identifier) : await deleteDeviceSms(panel.deviceId);
      if (res.State === ResponseState.UNAUTHORIZED) {
        // No manual navigation needed -- RootNavigator swaps to the login stack as soon as
        // session clears (see navigation/RootNavigator.tsx).
        logout();
        return;
      }
      if (res.State !== ResponseState.OK) {
        setError(res.Message || t('Could not unlink this panel.'));
        return;
      }
      // Ported from the previous app: unlinking a panel always force-logged-out the session
      // afterward, rather than just refreshing the panel list in place.
      await logout();
    } catch {
      setError(t('Could not reach the server.'));
    } finally {
      setWorking(false);
    }
  }

  function confirmBlockOrReset(action: 'Block' | 'Reset') {
    if (!panel) return;
    const title = action === 'Block' ? t('Lock this panel?') : t('Reset this panel?');
    const message =
      action === 'Block'
        ? t(
            'This immediately revokes app control of the panel and signs out everyone who has it linked, including you. To restore access, link it again with the same identifier and user code.',
          )
        : t(
            "This wipes the panel from your account entirely and signs out everyone who has it linked, including you. You'll need to pair it again from scratch.",
          );
    Alert.alert(title, message, [
      { text: t('Cancel'), style: 'cancel' },
      { text: action === 'Block' ? t('Lock') : t('Reset'), style: 'destructive', onPress: () => handleBlockOrReset(action) },
    ]);
  }

  async function handleBlockOrReset(action: 'Block' | 'Reset') {
    if (!panel) return;
    setError(null);
    setWorking(true);
    try {
      const res = await blockPin(panel.identifier, action);
      if (res.State !== ResponseState.OK && res.State !== ResponseState.UNAUTHORIZED) {
        setError(res.Message || t('Could not complete this action.'));
        return;
      }
      // Both actions revoke every linked account's tokens server-side, including the caller's --
      // the current session is already dead (whether this call reports OK or UNAUTHORIZED), so
      // log out locally rather than refreshSession().
      await logout();
    } catch {
      setError(t('Could not reach the server.'));
    } finally {
      setWorking(false);
    }
  }

  const hasPendingEdits = description !== savedDescription || identifier !== savedIdentifier || pin !== savedPin;

  return (
    <GradientBackground style={{ flex: 1 }}>
    <KeyboardAvoidingView style={{ flex: 1 }} behavior={Platform.OS === 'ios' ? 'padding' : 'height'}>
    <ScrollView contentContainerStyle={styles.container} keyboardShouldPersistTaps="handled">
      <PanelToolbar description={panel.description} onSwitchPanel={() => navigation.navigate('PanelSelector')} />
      <View style={{ height: spacing.lg }} />

      {error && <Banner kind="error">{error}</Banner>}

      <Card style={styles.card}>
        <FormField label={t('Description')} value={description} onChangeText={setDescription} />
        <FormField label={t('Identifier')} value={identifier} onChangeText={setIdentifier} autoCapitalize="none" autoCorrect={false} />
        <FormField label={t('User code')} value={pin} onChangeText={setPin} keyboardType="number-pad" />
      </Card>

      <View style={{ marginTop: spacing.lg }}>
        <PrimaryButton title={t('Unlink this panel')} onPress={confirmUnlink} loading={working} />
      </View>
      <View style={styles.bottomRow}>
        <View style={{ flex: 1, marginRight: spacing.sm }}>
          <PrimaryButton title={t('Update')} onPress={handleUpdate} loading={updating} disabled={!hasPendingEdits} />
        </View>
        <View style={{ flex: 1 }}>
          <PrimaryButton title={t('Cancel')} onPress={resetFields} tone="outline" disabled={!hasPendingEdits} />
        </View>
      </View>

      {panel.kind === 'ip' && (
        <>
          <View style={{ height: spacing.xl }} />
          <Text style={[typography.label, { color: colors.onDarkDim, marginBottom: spacing.sm }]}>{t('Access code lockout')}</Text>
          <Text style={[typography.bodyDim, { color: colors.onDarkDim, marginBottom: spacing.md }]}>
            {t('These affect everyone who has this panel linked, not just you.')}
          </Text>
          <PrimaryButton title={t('Lock this panel')} onPress={() => confirmBlockOrReset('Block')} loading={working} />
          <View style={{ height: spacing.sm }} />
          <PrimaryButton title={t('Reset this panel')} onPress={() => confirmBlockOrReset('Reset')} loading={working} />
        </>
      )}
    </ScrollView>
    </KeyboardAvoidingView>
    </GradientBackground>
  );
}

const styles = StyleSheet.create({
  container: {
    flexGrow: 1,
    padding: 24,
  },
  centered: {
    flex: 1,
    alignItems: 'center',
    justifyContent: 'center',
    padding: 24,
  },
  card: {
    alignItems: 'stretch',
  },
  bottomRow: {
    flexDirection: 'row',
    marginTop: 12,
  },
});

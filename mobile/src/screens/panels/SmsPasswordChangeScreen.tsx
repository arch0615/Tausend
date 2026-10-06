import { useState } from 'react';
import { Alert, KeyboardAvoidingView, Platform, ScrollView, StyleSheet, Text, View } from 'react-native';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import { useTheme } from '../../theme/ThemeProvider';
import { GradientBackground } from '../../components/GradientBackground';
import { useLocale } from '../../i18n/LocaleContext';
import { useAuth } from '../../auth/AuthContext';
import { usePanels } from '../../panels/PanelContext';
import { SmsCommands } from '../../pairing/smsCommands';
import { openSmsComposer } from '../../pairing/smsIntent';
import { updateDeviceSms } from '../../api/device';
import { ResponseState } from '../../api/types';
import { FormField } from '../../components/FormField';
import { PrimaryButton } from '../../components/PrimaryButton';
import { Banner } from '../../components/Banner';
import type { MainStackParamList } from '../../navigation/types';

type Props = NativeStackScreenProps<MainStackParamList, 'SmsPasswordChange'>;

// SMS-panel-only, ported from the previous app's sms-password-change.page. The SMS command
// changes the PIN on the panel itself, but the app has no way to confirm that actually happened
// (no reply parsing) -- so, matching legacy, the user is asked to confirm it worked before the
// new PIN is saved server-side. Saying "no" leaves the stored PIN untouched so later commands
// keep using the still-valid old one.
export function SmsPasswordChangeScreen(_props: Props) {
  const { colors, typography, spacing } = useTheme();
  const { t } = useLocale();
  const { session, logout } = useAuth();
  const { selected } = usePanels();
  const [pin, setPin] = useState('');
  const [confirmPin, setConfirmPin] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [sending, setSending] = useState(false);
  const [saving, setSaving] = useState(false);

  const smsDevice =
    selected?.kind === 'sms' ? session?.account.SmsDevices.find((d) => d.DeviceId === selected.deviceId) : undefined;

  async function handleSend() {
    if (!smsDevice) {
      setError(t('Select an SMS panel first.'));
      return;
    }
    if (!/^\d{1,4}$/.test(pin)) {
      setError(t('Enter a valid SMS PIN.'));
      return;
    }
    if (!/^\d{1,4}$/.test(confirmPin)) {
      setError(t('Enter a valid confirmation PIN.'));
      return;
    }
    if (pin !== confirmPin) {
      setError(t('The PINs do not match.'));
      return;
    }
    setError(null);
    setSending(true);
    try {
      const opened = await openSmsComposer(smsDevice.PhoneNumber, SmsCommands.changePassword(smsDevice.SimPin, pin));
      if (!opened) {
        setError(t('Could not open your messaging app.'));
        return;
      }
      Alert.alert(t('Attention'), t('Was the SMS PIN changed successfully on the panel?'), [
        { text: t('No'), style: 'cancel' },
        { text: t('Yes'), onPress: confirmChanged },
      ]);
    } catch {
      setError(t('Could not open your messaging app.'));
    } finally {
      setSending(false);
    }
  }

  async function confirmChanged() {
    if (!smsDevice) return;
    setSaving(true);
    setError(null);
    try {
      const res = await updateDeviceSms(
        smsDevice.DeviceId,
        smsDevice.Description,
        smsDevice.Identifier,
        smsDevice.DevicePin,
        pin,
        smsDevice.PhoneNumber,
        smsDevice.DeviceType,
      );
      if (res.State === ResponseState.UNAUTHORIZED) {
        // No manual navigation needed -- RootNavigator swaps to the login stack as soon as
        // session clears (see navigation/RootNavigator.tsx).
        logout();
        return;
      }
      if (res.State !== ResponseState.OK) {
        setError(res.Message || t('Could not update the panel.'));
        return;
      }
      // Ported from the previous app: saving the new SMS PIN always force-logged-out the
      // session afterward (a fresh login re-fetches the account's now-updated device list).
      await logout();
    } catch {
      setError(t('Could not reach the server.'));
    } finally {
      setSaving(false);
    }
  }

  return (
    <GradientBackground style={{ flex: 1 }}>
    <KeyboardAvoidingView
      style={{ flex: 1 }}
      behavior={Platform.OS === 'ios' ? 'padding' : 'height'}
    >
      <ScrollView contentContainerStyle={styles.container} keyboardShouldPersistTaps="handled">
        <Text style={[typography.title, { color: colors.onDark, marginBottom: spacing.xs }]}>{t('Change SMS PIN')}</Text>
        <Text style={[typography.bodyDim, { color: colors.onDarkDim, marginBottom: spacing.lg }]}>
          {smsDevice
            ? t('Change the SMS PIN on "{name}" by text message.', { name: smsDevice.Description })
            : t('Select an SMS panel to change its PIN.')}
        </Text>

        {error && <Banner kind="error">{error}</Banner>}

        <FormField labelColor={colors.onDarkDim}
          label={t('SMS PIN')}
          value={pin}
          onChangeText={setPin}
          keyboardType="number-pad"
          secureTextEntry
          maxLength={4}
        />
        <FormField labelColor={colors.onDarkDim}
          label={t('Repeat SMS PIN')}
          value={confirmPin}
          onChangeText={setConfirmPin}
          keyboardType="number-pad"
          secureTextEntry
          maxLength={4}
        />

        <View style={{ marginTop: spacing.sm }}>
          <PrimaryButton title={t('Send')} onPress={handleSend} loading={sending || saving} disabled={!smsDevice} />
        </View>
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
});

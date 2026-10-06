import { useEffect, useState } from 'react';
import { Alert, KeyboardAvoidingView, Platform, ScrollView, StyleSheet, Text, View } from 'react-native';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import { useTheme } from '../../theme/ThemeProvider';
import { GradientBackground } from '../../components/GradientBackground';
import { useLocale } from '../../i18n/LocaleContext';
import { useAuth } from '../../auth/AuthContext';
import { createDevice } from '../../api/device';
import { ResponseState } from '../../api/types';
import { FormField } from '../../components/FormField';
import { PrimaryButton } from '../../components/PrimaryButton';
import { Banner } from '../../components/Banner';
import { LoadingOverlay } from '../../components/LoadingOverlay';
import { getPinRetryCount, incrementPinRetryCount, resetPinRetryCount, MAX_PIN_RETRIES } from '../../pairing/pinRetryStore';
import type { MainStackParamList } from '../../navigation/types';

type Props = NativeStackScreenProps<MainStackParamList, 'CreateDevice'>;

// Exact string backend-core's DeviceController.CreateDevice returns for a wrong PIN (as opposed
// to "Central offline o inexistente", which shares the same Code 400 but shouldn't count against
// the lockout below -- see Business/DeviceBusiness.cs's ValidatePinOnDevice).
const WRONG_PIN_MESSAGE = 'PIN incorrecto';

export function CreateDeviceScreen({ navigation }: Props) {
  const { colors, typography, spacing } = useTheme();
  const { t } = useLocale();
  const { session, refreshSession, logout } = useAuth();
  const [description, setDescription] = useState('');
  const [identifier, setIdentifier] = useState('');
  const [pin, setPin] = useState('');
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [lockedOut, setLockedOut] = useState(false);

  useEffect(() => {
    (async () => {
      try {
        const count = await getPinRetryCount();
        if (count >= MAX_PIN_RETRIES) setLockedOut(true);
      } catch {
        // Fail open -- if the local retry count can't be read, don't falsely lock the user out.
      }
    })();
  }, []);

  function handleSubmit() {
    if (!description.trim()) {
      setError(t('Enter a name for this panel.'));
      return;
    }
    if (!identifier.trim()) {
      setError(t("Enter the panel's identifier."));
      return;
    }
    if (!/^\d{4}$/.test(pin)) {
      setError(t('The PIN must be 4 digits.'));
      return;
    }
    setError(null);
    // Ported from the previous app's setup.page.ts: linking a panel always force-logs-out the
    // session afterward (see PairingConfirmationScreen), so this confirms with the installer
    // before making the call rather than surprising them with it.
    Alert.alert(t('Attention'), t('This will log you out automatically after updating the panel.'), [
      { text: t('Cancel'), style: 'cancel' },
      { text: t('Continue'), onPress: doSubmit },
    ]);
  }

  async function doSubmit() {
    setSubmitting(true);
    try {
      // Live-validates against the panel through the relay -- can take several seconds,
      // see DeviceBusiness.ValidatePinOnDevice.
      // The previous app's event history showed which account's email just linked a panel
      // ("Usuario {email} ha vinculado su dispositivo a la Central."), matching
      // UserLinkedNotification.cs's GetBody() -- but this call never sent Email at all, so it
      // always rendered blank.
      const res = await createDevice(description.trim(), identifier.trim(), pin, session?.account.Email);
      if (res.Code === 0 && res.State === ResponseState.OK) {
        await resetPinRetryCount();
        // So the new panel shows up immediately in the selector/Home instead of waiting for
        // the next full app launch (session.account.Devices is otherwise only refreshed then).
        await refreshSession();
        navigation.replace('PairingConfirmation', { path: 'wifi' });
        return;
      }
      if (res.State === ResponseState.UNAUTHORIZED) {
        // No manual navigation needed -- RootNavigator swaps to the login stack as soon as
        // session clears (see navigation/RootNavigator.tsx).
        logout();
        return;
      }
      if (res.Code === 400 && res.Message === WRONG_PIN_MESSAGE) {
        const count = await incrementPinRetryCount();
        const left = Math.max(0, MAX_PIN_RETRIES - count);
        if (count >= MAX_PIN_RETRIES) {
          setLockedOut(true);
          setError(t('Too many incorrect attempts. Double-check the panel details before trying again.'));
        } else {
          setError(
            t(left === 1 ? 'Incorrect PIN. {left} attempt left.' : 'Incorrect PIN. {left} attempts left.', { left }),
          );
        }
        return;
      }
      // Other failures (panel offline, duplicate name, etc.) aren't the user mistyping a PIN --
      // don't count them against the lockout.
      setError(res.Message || t('Could not link the panel.'));
    } catch {
      setError(t('Could not reach the server.'));
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <>
    <GradientBackground style={{ flex: 1 }}>
    <KeyboardAvoidingView
      style={{ flex: 1 }}
      behavior={Platform.OS === 'ios' ? 'padding' : 'height'}
    >
      <ScrollView contentContainerStyle={styles.container} keyboardShouldPersistTaps="handled">
        <Text style={[typography.title, { color: colors.onDark, marginBottom: spacing.xs }]}>{t('Link this panel')}</Text>
        <Text style={[typography.bodyDim, { color: colors.onDarkDim, marginBottom: spacing.lg }]}>
          {t("Enter the panel's identifier and its 4-digit user PIN to add it to your account.")}
        </Text>

        {error && <Banner kind="error">{error}</Banner>}

        {lockedOut ? (
          <Banner kind="error">
            {t("Too many incorrect PIN attempts. Go back and confirm the panel's identifier and PIN before trying again.")}
          </Banner>
        ) : (
          <>
            <FormField labelColor={colors.onDarkDim} label={t('Panel description')} value={description} onChangeText={setDescription} />
            <FormField labelColor={colors.onDarkDim}
              label={t('Panel identifier')}
              value={identifier}
              onChangeText={setIdentifier}
              autoCapitalize="none"
              autoCorrect={false}
            />
            <FormField labelColor={colors.onDarkDim}
              label={t('User code (4 digits)')}
              value={pin}
              onChangeText={setPin}
              keyboardType="number-pad"
              secureTextEntry
              maxLength={4}
            />

            <View style={[styles.bottomRow, { marginTop: spacing.sm }]}>
              <View style={{ flex: 1, marginRight: spacing.sm }}>
                <PrimaryButton title={t('Add equipment')} onPress={handleSubmit} disabled={submitting} tone="dark" />
              </View>
              <View style={{ flex: 1 }}>
                <PrimaryButton title={t('Cancel')} onPress={() => navigation.goBack()} tone="outline" disabled={submitting} />
              </View>
            </View>
          </>
        )}
      </ScrollView>
    </KeyboardAvoidingView>
    </GradientBackground>
    <LoadingOverlay visible={submitting} label={t('Updating')} />
    </>
  );
}

const styles = StyleSheet.create({
  container: {
    flexGrow: 1,
    padding: 24,
  },
  bottomRow: {
    flexDirection: 'row',
  },
});

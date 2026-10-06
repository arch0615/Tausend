import { useState } from 'react';
import { Alert, KeyboardAvoidingView, Platform, Pressable, ScrollView, StyleSheet, Text, View } from 'react-native';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import { useTheme } from '../../theme/ThemeProvider';
import { GradientBackground } from '../../components/GradientBackground';
import { useLocale } from '../../i18n/LocaleContext';
import { useAuth } from '../../auth/AuthContext';
import { createDeviceSms } from '../../api/device';
import { ResponseState } from '../../api/types';
import { FormField } from '../../components/FormField';
import { PrimaryButton } from '../../components/PrimaryButton';
import { Banner } from '../../components/Banner';
import { LoadingOverlay } from '../../components/LoadingOverlay';
import type { MainStackParamList } from '../../navigation/types';

type Props = NativeStackScreenProps<MainStackParamList, 'SmsPairing'>;

// SMS panel models seen in the field -- the model string doubles as both the device Identifier
// and DeviceType on the backend (see api/device.ts createDeviceSms and the previous app's
// setup.page.ts handleSMS()).
const MODELS = [
  { value: 'cr800', label: 'CR800' },
  { value: 'cr832', label: 'CR832' },
];

export function SmsPairingScreen({ navigation }: Props) {
  const { colors, typography, spacing, radius } = useTheme();
  const { t } = useLocale();
  const { refreshSession, logout } = useAuth();
  const [model, setModel] = useState<string | null>(null);
  const [description, setDescription] = useState('');
  const [phone, setPhone] = useState('');
  const [simPin, setSimPin] = useState('');
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  function handleSubmit() {
    if (!model) {
      setError(t('Select your panel model.'));
      return;
    }
    if (!description.trim()) {
      setError(t('Enter a name for this panel.'));
      return;
    }
    if (phone.trim().length < 10) {
      setError(t('Enter a valid phone number for the panel’s SIM card.'));
      return;
    }
    if (!/^\d{4}$/.test(simPin)) {
      setError(t('The SIM PIN must be 4 digits.'));
      return;
    }
    setError(null);
    // Ported from the previous app's setup.page.ts setup() -- the same "you'll be logged out"
    // confirmation as the IP panel form (CreateDeviceScreen), shown for SMS panels too.
    Alert.alert(t('Attention'), t('This will log you out automatically after updating the panel.'), [
      { text: t('Cancel'), style: 'cancel' },
      { text: t('Continue'), onPress: doSubmit },
    ]);
  }

  async function doSubmit() {
    if (!model) return;
    setSubmitting(true);
    try {
      const res = await createDeviceSms(description.trim(), model, simPin, simPin, phone.trim(), model);
      if (res.Code === 0 && res.State === ResponseState.OK) {
        // So the new panel shows up immediately in the selector/Home instead of waiting for
        // the next full app launch (session.account.SmsDevices is otherwise only refreshed then).
        await refreshSession();
        navigation.replace('PairingConfirmation', { path: 'sms' });
        return;
      }
      if (res.State === ResponseState.UNAUTHORIZED) {
        // No manual navigation needed -- RootNavigator swaps to the login stack as soon as
        // session clears (see navigation/RootNavigator.tsx).
        logout();
        return;
      }
      if (res.Code === 2000) {
        setError(t('You already have a panel with that name.'));
        return;
      }
      setError(res.Message || t('Could not register the panel.'));
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
        <Text style={[typography.title, { color: colors.onDark, marginBottom: spacing.xs }]}>
          {t('Add an SMS panel')}
        </Text>
        <Text style={[typography.bodyDim, { color: colors.onDarkDim, marginBottom: spacing.lg }]}>
          {t('Enter the phone number of the SIM card installed in the panel, and its SMS PIN (set by the installer).')}
        </Text>

        {error && <Banner kind="error">{error}</Banner>}

        <Text style={[typography.label, { color: colors.onDarkDim, marginBottom: spacing.xs }]}>{t('Panel model')}</Text>
        <View style={[styles.modelRow, { marginBottom: spacing.md }]}>
          {MODELS.map((m) => {
            const selected = model === m.value;
            return (
              <Pressable
                key={m.value}
                onPress={() => setModel(m.value)}
                style={[
                  styles.modelOption,
                  {
                    borderColor: selected ? colors.accent : colors.line,
                    backgroundColor: selected ? colors.accent : colors.panel,
                    borderRadius: radius.sm,
                  },
                ]}
              >
                {/* Model labels (CR800/CR832) are product model names, not UI copy -- left untranslated. */}
                <Text style={[typography.body, { color: selected ? colors.accentInk : colors.ink }]}>{m.label}</Text>
              </Pressable>
            );
          })}
        </View>

        <FormField labelColor={colors.onDarkDim} label={t('Panel name')} value={description} onChangeText={setDescription} />
        <FormField labelColor={colors.onDarkDim} label={t('SIM phone number')} value={phone} onChangeText={setPhone} keyboardType="phone-pad" />
        <FormField labelColor={colors.onDarkDim} label={t('SIM PIN (4 digits)')} value={simPin} onChangeText={setSimPin} keyboardType="number-pad" secureTextEntry maxLength={4} />

        <View style={[styles.bottomRow, { marginTop: spacing.sm }]}>
          <View style={{ flex: 1, marginRight: spacing.sm }}>
            <PrimaryButton title={t('Add equipment')} onPress={handleSubmit} disabled={submitting} tone="dark" />
          </View>
          <View style={{ flex: 1 }}>
            <PrimaryButton title={t('Cancel')} onPress={() => navigation.goBack()} tone="outline" disabled={submitting} />
          </View>
        </View>
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
  modelRow: {
    flexDirection: 'row',
    gap: 12,
  },
  modelOption: {
    flex: 1,
    borderWidth: 1,
    paddingVertical: 12,
    alignItems: 'center',
  },
  bottomRow: {
    flexDirection: 'row',
  },
});

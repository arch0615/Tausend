import { useState } from 'react';
import { KeyboardAvoidingView, Platform, ScrollView, StyleSheet, Text, View } from 'react-native';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import { useTheme } from '../../theme/ThemeProvider';
import { GradientBackground } from '../../components/GradientBackground';
import { useLocale } from '../../i18n/LocaleContext';
import { useAuth } from '../../auth/AuthContext';
import { usePanels } from '../../panels/PanelContext';
import { SmsCommands } from '../../pairing/smsCommands';
import { openSmsComposer } from '../../pairing/smsIntent';
import { FormField } from '../../components/FormField';
import { PrimaryButton } from '../../components/PrimaryButton';
import { Banner } from '../../components/Banner';
import type { MainStackParamList } from '../../navigation/types';

type Props = NativeStackScreenProps<MainStackParamList, 'PanelIdentification'>;

// SMS-panel-only, ported from the previous app's identification screen: sets a human-readable
// name on the panel itself via the IDF SMS command (src/pairing/smsCommands.ts), not a local
// details/info screen. Reached only when the currently selected panel is an SMS panel -- see
// HomeScreen's conditional entry point.
export function PanelIdentificationScreen({ navigation }: Props) {
  const { colors, typography, spacing } = useTheme();
  const { t } = useLocale();
  const { session } = useAuth();
  const { selected } = usePanels();
  const [name, setName] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [sending, setSending] = useState(false);

  const smsDevice =
    selected?.kind === 'sms' ? session?.account.SmsDevices.find((d) => d.DeviceId === selected.deviceId) : undefined;

  async function handleSend() {
    if (!smsDevice) {
      setError(t('Select an SMS panel first.'));
      return;
    }
    if (!name.trim()) {
      setError(t('Enter a name for your panel.'));
      return;
    }
    setError(null);
    setSending(true);
    try {
      const command = SmsCommands.identification(smsDevice.SimPin, name.trim());
      const opened = await openSmsComposer(smsDevice.PhoneNumber, command);
      if (!opened) {
        setError(t('Could not open your messaging app.'));
        return;
      }
      navigation.goBack();
    } catch {
      setError(t('Could not open your messaging app.'));
    } finally {
      setSending(false);
    }
  }

  return (
    <GradientBackground style={{ flex: 1 }}>
    <KeyboardAvoidingView
      style={{ flex: 1 }}
      behavior={Platform.OS === 'ios' ? 'padding' : 'height'}
    >
      <ScrollView contentContainerStyle={styles.container} keyboardShouldPersistTaps="handled">
        <Text style={[typography.title, { color: colors.onDark, marginBottom: spacing.xs }]}>
          {t('Identify this panel')}
        </Text>
        <Text style={[typography.bodyDim, { color: colors.onDarkDim, marginBottom: spacing.lg }]}>
          {smsDevice
            ? t('Send a name to "{name}" by text message.', { name: smsDevice.Description })
            : t('Select an SMS panel to give it a name.')}
        </Text>

        {error && <Banner kind="error">{error}</Banner>}

        <FormField labelColor={colors.onDarkDim} label={t('Name to identify your alarm')} value={name} onChangeText={setName} />

        <View style={{ marginTop: spacing.sm }}>
          <PrimaryButton title={t('Send')} onPress={handleSend} loading={sending} disabled={!smsDevice} />
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

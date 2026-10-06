import { useState } from 'react';
import { KeyboardAvoidingView, Platform, Pressable, ScrollView, StyleSheet, Text, View } from 'react-native';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import { useAuth } from '../../auth/AuthContext';
import { useTheme } from '../../theme/ThemeProvider';
import { GradientBackground } from '../../components/GradientBackground';
import { useLocale } from '../../i18n/LocaleContext';
import { usePanels } from '../../panels/PanelContext';
import { SmsCommands } from '../../pairing/smsCommands';
import { openSmsComposer } from '../../pairing/smsIntent';
import { FormField } from '../../components/FormField';
import { PrimaryButton } from '../../components/PrimaryButton';
import { Banner } from '../../components/Banner';
import { PanelToolbar } from '../../components/PanelToolbar';
import type { MainStackParamList } from '../../navigation/types';

type Props = NativeStackScreenProps<MainStackParamList, 'CustomMessages'>;

const MIN_SLOT = 51;
const MAX_SLOT = 70;
const MAX_MESSAGE_LENGTH = 50;

type Mode = 'read' | 'write';

// Legacy app (custommessages.page.ts) led with a read/write mode picker, defaulting to read --
// only the field relevant to the chosen mode appeared, followed by a single submit button --
// rather than always showing both fields and both buttons at once. Restoring that flow here
// while keeping this app's own chip styling (see ScheduledDeparturesScreen.tsx's day-of-week/
// action chips).
const MODES: { key: Mode; label: string }[] = [
  { key: 'read', label: 'Read message' },
  { key: 'write', label: 'Write message' },
];

// SMS-panel-only, no IP/backend equivalent exists anywhere in the system -- confirmed by
// exhaustive search of both backends' command vocabularies. Writes/reads a free-text slot
// directly in the panel's own memory (SML/SMU opcodes); no DB persistence, no relation to zone
// numbers (the panel supports 20 slots, numbered 51-70, an unrelated range). Read-only reply
// arrives as a plain SMS in the phone's own messaging app, not surfaced in this screen.
export function CustomMessagesScreen({ navigation }: Props) {
  const { colors, typography, spacing, radius } = useTheme();
  const { t } = useLocale();
  const { session } = useAuth();
  const { selected } = usePanels();
  const [mode, setMode] = useState<Mode>('read');
  const [slot, setSlot] = useState('');
  const [message, setMessage] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [sending, setSending] = useState(false);

  const smsDevice =
    selected?.kind === 'sms' ? session?.account.SmsDevices.find((d) => d.DeviceId === selected.deviceId) : undefined;

  function parseSlot(): number | null {
    const num = Number(slot.trim());
    if (!Number.isInteger(num) || num < MIN_SLOT || num > MAX_SLOT) {
      setError(t('Enter a message number between {min} and {max}.', { min: MIN_SLOT, max: MAX_SLOT }));
      return null;
    }
    return num;
  }

  async function send(command: string) {
    if (!smsDevice) {
      setError(t('Select an SMS panel first.'));
      return;
    }
    setError(null);
    setSending(true);
    try {
      const opened = await openSmsComposer(smsDevice.PhoneNumber, command);
      if (!opened) setError(t('Could not open your messaging app.'));
    } catch {
      setError(t('Could not open your messaging app.'));
    } finally {
      setSending(false);
    }
  }

  function handleRead() {
    const num = parseSlot();
    if (num === null || !smsDevice) return;
    send(SmsCommands.readMessage(smsDevice.SimPin, num));
  }

  function handleWrite() {
    const num = parseSlot();
    if (num === null || !smsDevice) return;
    const trimmed = message.trim();
    if (!trimmed) {
      setError(t('Enter the message text.'));
      return;
    }
    if (trimmed.length > MAX_MESSAGE_LENGTH) {
      setError(t('Message must be {max} characters or fewer.', { max: MAX_MESSAGE_LENGTH }));
      return;
    }
    send(SmsCommands.setMessage(smsDevice.SimPin, num, trimmed));
  }

  function handleSubmit() {
    if (mode === 'write') handleWrite();
    else handleRead();
  }

  return (
    <GradientBackground style={{ flex: 1 }}>
    <KeyboardAvoidingView
      style={{ flex: 1 }}
      behavior={Platform.OS === 'ios' ? 'padding' : 'height'}
    >
      <ScrollView contentContainerStyle={styles.container} keyboardShouldPersistTaps="handled">
        <PanelToolbar description={selected?.description ?? ''} onSwitchPanel={() => navigation.navigate('PanelSelector')} />

        <Text style={[typography.title, { color: colors.onDark, marginBottom: spacing.xs }]}>{t('Custom messages')}</Text>
        <Text style={[typography.bodyDim, { color: colors.onDarkDim, marginBottom: spacing.lg }]}>
          {smsDevice
            ? t('Read or write a message slot on "{name}" by text message.', { name: smsDevice.Description })
            : t('Select an SMS panel to manage its messages.')}
        </Text>

        {error && <Banner kind="error">{error}</Banner>}

        <View style={[styles.chipRow, { marginBottom: spacing.md }]}>
          {MODES.map((m) => {
            const active = mode === m.key;
            return (
              <Pressable
                key={m.key}
                onPress={() => setMode(m.key)}
                style={[
                  styles.chip,
                  {
                    borderColor: active ? colors.accent : colors.line,
                    backgroundColor: active ? colors.accent : colors.panel,
                    borderRadius: radius.pill,
                  },
                ]}
              >
                <Text style={[typography.bodyDim, { color: active ? colors.accentInk : colors.ink }]}>{t(m.label)}</Text>
              </Pressable>
            );
          })}
        </View>

        <FormField labelColor={colors.onDarkDim}
          label={t('Message number ({min}-{max})', { min: MIN_SLOT, max: MAX_SLOT })}
          value={slot}
          onChangeText={setSlot}
          keyboardType="number-pad"
        />
        {mode === 'write' && (
          <FormField labelColor={colors.onDarkDim}
            label={t('Message text (up to {max} characters)', { max: MAX_MESSAGE_LENGTH })}
            value={message}
            onChangeText={setMessage}
            maxLength={MAX_MESSAGE_LENGTH}
          />
        )}

        <View style={{ marginTop: spacing.sm }}>
          <PrimaryButton title={t('Send')} onPress={handleSubmit} loading={sending} disabled={!smsDevice} />
          <View style={{ height: spacing.sm }} />
          <Pressable onPress={() => navigation.goBack()} style={{ alignItems: 'center' }}>
            <Text style={[typography.bodyDim, { color: colors.inkDim }]}>{t('Cancel')}</Text>
          </Pressable>
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
  chipRow: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: 8,
  },
  chip: {
    borderWidth: 1,
    paddingVertical: 8,
    paddingHorizontal: 14,
  },
});

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

type Props = NativeStackScreenProps<MainStackParamList, 'ContactPhone'>;

const MIN_SLOT = 1;
const MAX_SLOT = 16;
const PHONE_PATTERN = /^[0-9]{10,16}$/;

type Mode = 'write' | 'read' | 'delete';

// Legacy app (contactphone.page.ts) led with a write/read/delete mode picker -- only the field
// relevant to the chosen mode appeared, followed by a single submit button -- rather than always
// showing every field and button at once. Restoring that flow here while keeping this app's own
// chip styling (see ScheduledDeparturesScreen.tsx's day-of-week/action chips).
const MODES: { key: Mode; label: string }[] = [
  { key: 'write', label: 'Write phone number' },
  { key: 'read', label: 'Read phone number' },
  { key: 'delete', label: 'Delete phone number' },
];

// SMS-panel-only, no IP/backend equivalent exists anywhere in the system (same as Custom
// Messages). Despite the "zone" naming in smsCommands.ts (inherited from the old app), this slot
// number (1-16) has no relation to the panel's 32 alarm zones -- it's just an independent
// contact-number list index. Writes/reads/deletes directly in the panel's own memory via SMS; no
// DB persistence, no in-app reply parsing.
export function ContactPhoneScreen({ navigation }: Props) {
  const { colors, typography, spacing, radius } = useTheme();
  const { t } = useLocale();
  const { session } = useAuth();
  const { selected } = usePanels();
  const [mode, setMode] = useState<Mode>('write');
  const [slot, setSlot] = useState('');
  const [phone, setPhone] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [sending, setSending] = useState(false);

  const smsDevice =
    selected?.kind === 'sms' ? session?.account.SmsDevices.find((d) => d.DeviceId === selected.deviceId) : undefined;

  function parseSlot(): number | null {
    const num = Number(slot.trim());
    if (!Number.isInteger(num) || num < MIN_SLOT || num > MAX_SLOT) {
      setError(t('Enter an order number between {min} and {max}.', { min: MIN_SLOT, max: MAX_SLOT }));
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

  function handleSave() {
    const num = parseSlot();
    if (num === null || !smsDevice) return;
    const trimmed = phone.trim();
    if (!PHONE_PATTERN.test(trimmed)) {
      setError(t('Enter a phone number of 10 to 16 digits, no spaces or symbols.'));
      return;
    }
    send(SmsCommands.setContactNumber(smsDevice.SimPin, num, trimmed));
  }

  function handleRead() {
    const num = parseSlot();
    if (num === null || !smsDevice) return;
    send(SmsCommands.readContactNumber(smsDevice.SimPin, num));
  }

  function handleDelete() {
    const num = parseSlot();
    if (num === null || !smsDevice) return;
    send(SmsCommands.deleteContactNumber(smsDevice.SimPin, num));
  }

  function handleSubmit() {
    if (mode === 'write') handleSave();
    else if (mode === 'read') handleRead();
    else handleDelete();
  }

  return (
    <GradientBackground style={{ flex: 1 }}>
    <KeyboardAvoidingView
      style={{ flex: 1 }}
      behavior={Platform.OS === 'ios' ? 'padding' : 'height'}
    >
      <ScrollView contentContainerStyle={styles.container} keyboardShouldPersistTaps="handled">
        <PanelToolbar description={selected?.description ?? ''} onSwitchPanel={() => navigation.navigate('PanelSelector')} />

        <Text style={[typography.title, { color: colors.onDark, marginBottom: spacing.xs }]}>{t('Contact phone numbers')}</Text>
        <Text style={[typography.bodyDim, { color: colors.onDarkDim, marginBottom: spacing.lg }]}>
          {smsDevice
            ? t('Manage a contact number on "{name}" by text message.', { name: smsDevice.Description })
            : t('Select an SMS panel to manage its contact numbers.')}
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
          label={t('Order number ({min}-{max})', { min: MIN_SLOT, max: MAX_SLOT })}
          value={slot}
          onChangeText={setSlot}
          keyboardType="number-pad"
        />
        {mode === 'write' && (
          <FormField labelColor={colors.onDarkDim}
            label={t('Phone number (10-16 digits)')}
            value={phone}
            onChangeText={setPhone}
            keyboardType="phone-pad"
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

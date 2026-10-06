import { useState } from 'react';
import { Pressable, ScrollView, StyleSheet, Text } from 'react-native';
import { useTheme } from '../../theme/ThemeProvider';
import { GradientBackground } from '../../components/GradientBackground';
import { useLocale } from '../../i18n/LocaleContext';
import { useAuth } from '../../auth/AuthContext';
import { usePanels } from '../../panels/PanelContext';
import { SmsCommands } from '../../pairing/smsCommands';
import { openSmsComposer } from '../../pairing/smsIntent';
import { Banner } from '../../components/Banner';
import { ProgramControlList } from './ProgramControlList';

const SMS_OUTPUT_COUNT = 8;

export function PgmScreen() {
  const { t } = useLocale();
  const { selected } = usePanels();
  // The old app's single "Salidas Programables" screen covered both panel kinds under one menu
  // entry: a live on/off toggle list for IP panels (see ProgramControlList), and, for SMS panels
  // -- which have no live connection to read state from -- a fire-and-forget button per output
  // that just texts a PGM<n> command (scheduled-departures.page.ts's smsSchedule()). Restoring
  // that SMS branch here, which had no RN screen at all until now.
  if (selected?.kind === 'sms') {
    return <SmsProgramControlButtons />;
  }
  return <ProgramControlList labelPrefix={t('Output')} emptyNamePlaceholder={t('Outputs')} />;
}

function SmsProgramControlButtons() {
  const { colors, typography, spacing, radius } = useTheme();
  const { t } = useLocale();
  const { session } = useAuth();
  const { selected } = usePanels();
  const [error, setError] = useState<string | null>(null);
  const [sendingPgm, setSendingPgm] = useState<number | null>(null);

  const smsDevice =
    selected?.kind === 'sms' ? session?.account.SmsDevices.find((d) => d.DeviceId === selected.deviceId) : undefined;

  async function send(pgmNumber: number) {
    if (!smsDevice) return;
    setError(null);
    setSendingPgm(pgmNumber);
    try {
      const opened = await openSmsComposer(smsDevice.PhoneNumber, SmsCommands.scheduledDeparture(smsDevice.SimPin, pgmNumber));
      if (!opened) setError(t('Could not open your messaging app.'));
    } catch {
      setError(t('Could not open your messaging app.'));
    } finally {
      setSendingPgm(null);
    }
  }

  return (
    <GradientBackground style={{ flex: 1 }}>
    <ScrollView contentContainerStyle={styles.container}>
      <Text style={[typography.title, { color: colors.onDark, marginBottom: spacing.xs }]}>{t('PGM outputs')}</Text>
      <Text style={[typography.bodyDim, { color: colors.onDarkDim, marginBottom: spacing.lg }]}>
        {smsDevice
          ? t('Toggle an output on "{name}" by text message.', { name: smsDevice.Description })
          : t('Select an SMS panel to manage its outputs.')}
      </Text>

      {error && <Banner kind="error">{error}</Banner>}

      {Array.from({ length: SMS_OUTPUT_COUNT }, (_, i) => i + 1).map((pgmNumber) => (
        <Pressable
          key={pgmNumber}
          onPress={() => send(pgmNumber)}
          disabled={!smsDevice || sendingPgm !== null}
          style={[
            styles.row,
            {
              borderColor: colors.line,
              backgroundColor: colors.panel,
              borderRadius: radius.sm,
              marginBottom: spacing.sm,
              opacity: sendingPgm !== null && sendingPgm !== pgmNumber ? 0.6 : 1,
            },
          ]}
        >
          <Text style={[typography.body, { color: colors.ink }]}>{t('Output {n}', { n: pgmNumber })}</Text>
        </Pressable>
      ))}
    </ScrollView>
    </GradientBackground>
  );
}

const styles = StyleSheet.create({
  container: {
    flexGrow: 1,
    padding: 24,
  },
  row: {
    borderWidth: 1,
    padding: 16,
    alignItems: 'center',
  },
});

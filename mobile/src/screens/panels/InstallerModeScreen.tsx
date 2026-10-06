import { useState } from 'react';
import { KeyboardAvoidingView, Platform, ScrollView, StyleSheet, Text, View } from 'react-native';
import { useNavigation } from '@react-navigation/native';
import { useAuth } from '../../auth/AuthContext';
import { useTheme } from '../../theme/ThemeProvider';
import { GradientBackground } from '../../components/GradientBackground';
import { Card } from '../../components/Card';
import { useLocale } from '../../i18n/LocaleContext';
import { usePanels } from '../../panels/PanelContext';
import { sendInstallerCommand } from '../../api/command';
import { AccountRole, ResponseState } from '../../api/types';
import { FormField } from '../../components/FormField';
import { PrimaryButton } from '../../components/PrimaryButton';
import { Banner } from '../../components/Banner';
import { PanelToolbar } from '../../components/PanelToolbar';

// Raw command box for Installer/Admin accounts, wired to CommandService/SendInstallerCommand.
// The old app (instalator.page.ts) gated this same box with a PIN read off the panel's own
// memory (PRG003) via a "Contraseña requerida" dialog, compared client-side against that fetched
// value -- the Phase 2 roles model replaces that gate entirely, both here (client-side, so an
// EndUser never sees the box) and server-side (CommandController.SendInstallerCommand now checks
// IsInstallerOrAdmin and returns FORBIDDEN otherwise, so a captured EndUser token can't reach it
// either). New APP Videos/'s "INSTALADOR" screen (frames 3-4) still shows that legacy password
// prompt -- not restored here, since replacing it was a deliberate, already-approved security
// upgrade, not an oversight.
export function InstallerModeScreen() {
  const { colors, typography, spacing } = useTheme();
  const { t } = useLocale();
  const { session, logout } = useAuth();
  const { selected } = usePanels();
  const navigation = useNavigation();
  const deviceId = selected?.kind === 'ip' ? selected.deviceId : null;
  const [command, setCommand] = useState('');
  const [response, setResponse] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [sending, setSending] = useState(false);

  const role = session?.account.Role;
  const authorized = role === AccountRole.Installer || role === AccountRole.Admin;

  async function handleSend() {
    if (!deviceId || !command.trim()) return;
    setError(null);
    setResponse(null);
    setSending(true);
    try {
      const res = await sendInstallerCommand(deviceId, command.trim());
      if (res.State === ResponseState.UNAUTHORIZED || res.Code === 401) {
        logout();
        return;
      }
      if (res.State === ResponseState.FORBIDDEN) {
        setError(t('Your account is not authorized for Installer mode.'));
        return;
      }
      if (res.State === ResponseState.CENTRAL_UNRESPONSIVE) {
        setError(t("The panel isn't responding right now."));
        return;
      }
      setResponse(res.Text ?? t('(no response)'));
    } catch {
      setError(t('Could not reach the server.'));
    } finally {
      setSending(false);
    }
  }

  if (!authorized) {
    return (
      <GradientBackground style={styles.centered}>
        <Text style={[typography.bodyDim, { color: colors.onDarkDim, textAlign: 'center' }]}>
          {t('Your account is not authorized for Installer mode.')}
        </Text>
      </GradientBackground>
    );
  }

  if (!deviceId) {
    return (
      <GradientBackground style={styles.centered}>
        <Text style={[typography.bodyDim, { color: colors.onDarkDim }]}>{t('Select a Wi-Fi panel to use Installer mode.')}</Text>
      </GradientBackground>
    );
  }

  return (
    <GradientBackground style={{ flex: 1 }}>
    <KeyboardAvoidingView
      style={{ flex: 1 }}
      behavior={Platform.OS === 'ios' ? 'padding' : 'height'}
    >
      <ScrollView contentContainerStyle={styles.container} keyboardShouldPersistTaps="handled">
        <PanelToolbar
          description={selected?.description ?? ''}
          onSwitchPanel={() => navigation.navigate('PanelSelector' as never)}
        />

        <View style={{ marginTop: spacing.lg }}>
          {error && <Banner kind="error">{error}</Banner>}
          {response !== null && <Banner kind="ok">{response}</Banner>}

          <Card style={styles.card}>
            <FormField label={t('Command')} value={command} onChangeText={setCommand} autoCapitalize="characters" style={styles.underlineField} />
          </Card>
        </View>

        <View style={[styles.bottomRow, { marginTop: spacing.lg }]}>
          <View style={{ flex: 1, marginRight: spacing.sm }}>
            <PrimaryButton title={t('Send')} onPress={handleSend} loading={sending} disabled={!command.trim()} tone="dark" />
          </View>
          <View style={{ flex: 1 }}>
            <PrimaryButton title={t('Back')} onPress={() => navigation.goBack()} tone="outline" />
          </View>
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
  centered: {
    flex: 1,
    alignItems: 'center',
    justifyContent: 'center',
    padding: 24,
  },
  bottomRow: {
    flexDirection: 'row',
  },
  card: {
    alignItems: 'stretch',
  },
  // The reference shows "Comando" as an underlined field rather than FormField's usual bordered
  // box -- FormField's own default style is left alone everywhere else.
  underlineField: {
    borderWidth: 0,
    borderBottomWidth: 1,
    borderRadius: 0,
    paddingHorizontal: 0,
  },
});

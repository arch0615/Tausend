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
import { ResponseState } from '../../api/types';
import { FormField } from '../../components/FormField';
import { PrimaryButton } from '../../components/PrimaryButton';
import { Banner } from '../../components/Banner';
import { PanelToolbar } from '../../components/PanelToolbar';

// Raw command box, wired to CommandService/SendInstallerCommand.
//
// Gate: the panel's OWN installer code, read live from section 003 (PRG003) and compared against
// what the user types -- the same gate the old app used (instalator.page.ts's "Contraseña
// requerida" dialog).
//
// This replaced an account-role gate (Installer/Admin only). That was a deliberate Phase 2
// security upgrade, but it does not match how the client actually works: an installer is a
// technician visiting a site, using whoever's account is on the phone, and proving they are an
// installer by knowing the panel's code. Under the role gate the screen simply vanished for
// every normal account, which the client reported as issues #20 (option disappeared from the
// menu) and #4 (it must ask for the installer code and match section 003).
//
// What still protects this: the caller must own the panel (CommandController.DeviceUnauthorized),
// every command is written to the audit log, and the code itself never leaves the panel except
// in the reply to a caller who already owns it. What it no longer does is require a privileged
// account, which is the trade the client asked for.
export function InstallerModeScreen() {
  const { colors, typography, spacing } = useTheme();
  const { t } = useLocale();
  const { logout } = useAuth();
  const { selected } = usePanels();
  const navigation = useNavigation();
  const deviceId = selected?.kind === 'ip' ? selected.deviceId : null;
  const [command, setCommand] = useState('');
  const [response, setResponse] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [sending, setSending] = useState(false);
  // Unlock gate -- reset on every mount, so leaving the screen re-locks it.
  const [unlocked, setUnlocked] = useState(false);
  const [installerCode, setInstallerCode] = useState('');
  const [unlocking, setUnlocking] = useState(false);
  const [gateError, setGateError] = useState<string | null>(null);

  /** Pulls the installer code out of the panel's reply, e.g. "PRG003:1234" -> "1234". */
  function parseSectionValue(text: string): string {
    const afterColon = text.includes(':') ? text.slice(text.indexOf(':') + 1) : text;
    return afterColon.trim();
  }

  async function handleUnlock() {
    if (!deviceId || !installerCode.trim()) return;
    setGateError(null);
    setUnlocking(true);
    try {
      const res = await sendInstallerCommand(deviceId, 'PRG003');
      if (res.State === ResponseState.UNAUTHORIZED || res.Code === 401) {
        logout();
        return;
      }
      if (res.State === ResponseState.FORBIDDEN) {
        setGateError(t('Your account is not authorized for Installer mode.'));
        return;
      }
      if (res.State === ResponseState.CENTRAL_UNRESPONSIVE || !res.Text) {
        setGateError(t("The panel isn't responding right now."));
        return;
      }
      const onPanel = parseSectionValue(res.Text);
      // Tolerant of a comma-separated reply (PRG000 answers that way, see the client's own
      // screenshots) -- any one field matching counts.
      const matches = onPanel === installerCode.trim() || onPanel.split(',').some((part) => part.trim() === installerCode.trim());
      if (!matches) {
        setGateError(t('Incorrect installer code.'));
        return;
      }
      setInstallerCode('');
      setUnlocked(true);
    } catch {
      setGateError(t('Could not reach the server.'));
    } finally {
      setUnlocking(false);
    }
  }

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

  if (!deviceId) {
    return (
      <GradientBackground style={styles.centered}>
        <Text style={[typography.bodyDim, { color: colors.onDarkDim }]}>{t('Select an IP panel to use Installer mode.')}</Text>
      </GradientBackground>
    );
  }

  if (!unlocked) {
    return (
      <GradientBackground style={{ flex: 1 }}>
      <KeyboardAvoidingView style={{ flex: 1 }} behavior={Platform.OS === 'ios' ? 'padding' : 'height'}>
        <ScrollView contentContainerStyle={styles.container} keyboardShouldPersistTaps="handled">
          <PanelToolbar
            description={selected?.description ?? ''}
            onSwitchPanel={() => navigation.navigate('PanelSelector' as never)}
          />
          <View style={{ marginTop: spacing.lg }}>
            {gateError && <Banner kind="error">{gateError}</Banner>}
            <Card style={styles.card}>
              <Text style={[typography.bodyDim, { color: colors.inkDim, marginBottom: spacing.md }]}>
                {t('Enter the installer code configured on this panel (section 003).')}
              </Text>
              <FormField
                label={t('Installer code')}
                value={installerCode}
                onChangeText={setInstallerCode}
                secureTextEntry
                keyboardType="number-pad"
                style={styles.underlineField}
              />
            </Card>
          </View>
          <View style={[styles.bottomRow, { marginTop: spacing.lg }]}>
            <View style={{ flex: 1, marginRight: spacing.sm }}>
              <PrimaryButton
                title={t('Continue')}
                onPress={handleUnlock}
                loading={unlocking}
                disabled={!installerCode.trim()}
                tone="dark"
              />
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
          {/* Client issue #4, second half: the panel's reply was rendered in the same small
              bodyDim Banner used for every incidental message in the app, and an installer reads
              these codes off the screen while standing at the panel. Own larger, selectable
              block instead. */}
          {response !== null && (
            <View style={[styles.responseBox, { backgroundColor: colors.okBg, borderRadius: 8, marginBottom: spacing.md }]}>
              <Text selectable style={[styles.responseText, { color: colors.okInk }]}>
                {response}
              </Text>
            </View>
          )}

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
  responseBox: {
    padding: 16,
  },
  responseText: {
    fontSize: 20,
    fontWeight: '700',
    letterSpacing: 0.5,
  },
});

import { useState } from 'react';
import { Image, KeyboardAvoidingView, Platform, ScrollView, StyleSheet, Text, View } from 'react-native';
import { launchImageLibrary } from 'react-native-image-picker';
import { useNavigation } from '@react-navigation/native';
import { useTheme } from '../../theme/ThemeProvider';
import { GradientBackground } from '../../components/GradientBackground';
import { useLocale } from '../../i18n/LocaleContext';
import { setDeviceLogos } from '../../api/device';
import { ResponseState } from '../../api/types';
import { FormField } from '../../components/FormField';
import { PrimaryButton } from '../../components/PrimaryButton';
import { Banner } from '../../components/Banner';
import { LoadingOverlay } from '../../components/LoadingOverlay';

// Client, 26/27 Oct: an installer-facing screen, reachable from the main menu near Installer mode,
// that sets the white-label logos for a panel by its identifier. Gated by the exact same live
// identifier+pin credential CreateDeviceScreen uses to pair a panel (DeviceBusiness.
// ValidatePinOnDevice) -- not an account-ownership check, since this can be done before any
// account is linked to the panel at all. Two logos, set together in one call per the client's own
// spec: Logo1 (portrait) replaces the eagle on the login screen, Logo2 (landscape) replaces the
// "ALARMAS TAUSEND" banner at the top of the home screen. Once set, every account with that panel
// linked sees them instead of the default Tausend look.
export function SetPanelLogoScreen() {
  const { colors, typography, spacing } = useTheme();
  const { t } = useLocale();
  const navigation = useNavigation();
  const [identifier, setIdentifier] = useState('');
  const [pin, setPin] = useState('');
  const [logo1Base64, setLogo1Base64] = useState<string | null>(null);
  const [logo2Base64, setLogo2Base64] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState(false);
  const [submitting, setSubmitting] = useState(false);

  // Kept small on purpose: these images ride along in every login response for every account that
  // has this panel linked, not fetched on demand -- a multi-megabyte photo would bloat every one
  // of those logins for a logo nobody needs at full resolution.
  async function pickImage(onPicked: (base64: string) => void) {
    setError(null);
    const result = await launchImageLibrary({
      mediaType: 'photo',
      includeBase64: true,
      maxWidth: 512,
      maxHeight: 512,
      quality: 0.8,
    });
    if (result.didCancel) return;
    if (result.errorMessage || !result.assets?.[0]?.base64) {
      setError(t('Could not read that image.'));
      return;
    }
    onPicked(result.assets[0].base64);
  }

  function validate(): string | null {
    if (!identifier.trim()) return t("Enter the panel's identifier.");
    if (!/^\d{4}$/.test(pin)) return t('The PIN must be 4 digits.');
    if (!logo1Base64 || !logo2Base64) return t('Choose both logos first.');
    return null;
  }

  async function handleSubmit() {
    const validationError = validate();
    if (validationError) {
      setError(validationError);
      return;
    }
    setError(null);
    setSuccess(false);
    setSubmitting(true);
    try {
      // Live-validates against the panel through the relay, same as pairing -- can take several
      // seconds and can fail with "offline" as well as "wrong PIN".
      const res = await setDeviceLogos(identifier.trim(), pin, logo1Base64!, logo2Base64!);
      if (res.State === ResponseState.OK) {
        setSuccess(true);
        setIdentifier('');
        setPin('');
        setLogo1Base64(null);
        setLogo2Base64(null);
        return;
      }
      setError(res.Message || t('Could not set the logos.'));
    } catch {
      setError(t('Could not reach the server.'));
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <>
      <GradientBackground style={{ flex: 1 }}>
        <KeyboardAvoidingView style={{ flex: 1 }} behavior={Platform.OS === 'ios' ? 'padding' : 'height'}>
          <ScrollView contentContainerStyle={styles.container} keyboardShouldPersistTaps="handled">
            <Text style={[typography.title, { color: colors.onDark, marginBottom: spacing.xs }]}>
              {t('Set custom logo')}
            </Text>
            <Text style={[typography.bodyDim, { color: colors.onDarkDim, marginBottom: spacing.lg }]}>
              {t("Enter the panel's identifier and its 4-digit user PIN, then choose the two logos to show instead of the default Tausend look.")}
            </Text>

            {error && <Banner kind="error">{error}</Banner>}
            {success && <Banner kind="ok">{t('Logos saved. They will show the next time each linked account logs in.')}</Banner>}

            <FormField
              labelColor={colors.onDarkDim}
              label={t('Panel identifier')}
              value={identifier}
              onChangeText={setIdentifier}
              autoCapitalize="none"
              autoCorrect={false}
            />
            <FormField
              labelColor={colors.onDarkDim}
              label={t('User code (4 digits)')}
              value={pin}
              onChangeText={setPin}
              keyboardType="number-pad"
              secureTextEntry
              maxLength={4}
            />

            <Text style={[typography.label, { color: colors.onDarkDim, marginTop: spacing.lg, marginBottom: spacing.xs }]}>
              {t('Logo 1 (portrait, login screen)')}
            </Text>
            <PrimaryButton title={t('Choose image')} onPress={() => pickImage(setLogo1Base64)} tone="outline" disabled={submitting} />
            {logo1Base64 && (
              <View style={[styles.previewWrap, { marginTop: spacing.md }]}>
                <Image source={{ uri: `data:image/jpeg;base64,${logo1Base64}` }} style={styles.previewPortrait} resizeMode="contain" />
              </View>
            )}

            <Text style={[typography.label, { color: colors.onDarkDim, marginTop: spacing.lg, marginBottom: spacing.xs }]}>
              {t('Logo 2 (landscape, home screen banner)')}
            </Text>
            <PrimaryButton title={t('Choose image')} onPress={() => pickImage(setLogo2Base64)} tone="outline" disabled={submitting} />
            {logo2Base64 && (
              <View style={[styles.previewWrap, { marginTop: spacing.md }]}>
                <Image source={{ uri: `data:image/jpeg;base64,${logo2Base64}` }} style={styles.previewLandscape} resizeMode="contain" />
              </View>
            )}

            <View style={{ marginTop: spacing.lg }}>
              <PrimaryButton title={t('Save logos')} onPress={handleSubmit} loading={submitting} disabled={submitting} tone="dark" />
            </View>
            <View style={{ marginTop: spacing.sm }}>
              <PrimaryButton title={t('Back')} onPress={() => navigation.goBack()} tone="outline" disabled={submitting} />
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
  previewWrap: {
    alignItems: 'center',
  },
  previewPortrait: {
    width: 120,
    height: 160,
  },
  previewLandscape: {
    width: 220,
    height: 80,
  },
});

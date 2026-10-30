import { useState } from 'react';
import { Image, KeyboardAvoidingView, Platform, ScrollView, StyleSheet, Text, View } from 'react-native';
import { launchImageLibrary } from 'react-native-image-picker';
import { useNavigation } from '@react-navigation/native';
import { useTheme } from '../../theme/ThemeProvider';
import { GradientBackground } from '../../components/GradientBackground';
import { useLocale } from '../../i18n/LocaleContext';
import { useAuth } from '../../auth/AuthContext';
import { clearDeviceLogos, setDeviceLogos, type LogoUpload } from '../../api/device';
import { ResponseState } from '../../api/types';
import { FormField } from '../../components/FormField';
import { PrimaryButton } from '../../components/PrimaryButton';
import { Banner } from '../../components/Banner';
import { LoadingOverlay } from '../../components/LoadingOverlay';
import { logoDataUri } from '../../branding/logoUri';

// Client, 26/27 Oct: sets the white-label logos for a panel by its identifier. Reached from
// Installer mode, which is itself behind the panel's installer code: "images can only be changed
// via the installer screen" (client, 30 Oct). Gated again here by the same live identifier+pin
// credential CreateDeviceScreen uses to pair a panel (DeviceBusiness.ValidatePinOnDevice) -- not
// an account-ownership check, since this can be done before any account is linked to the panel.
// Logo1 (portrait) is the splash screen; Logo2 (landscape) is the banner on that panel's own home
// screen. Either can be set on its own, and both can be dropped again with the reset button.
export function SetPanelLogoScreen() {
  const { colors, typography, spacing } = useTheme();
  const { t } = useLocale();
  const { refreshSession } = useAuth();
  const navigation = useNavigation();
  const [identifier, setIdentifier] = useState('');
  const [pin, setPin] = useState('');
  const [logo1, setLogo1] = useState<LogoUpload | null>(null);
  const [logo2, setLogo2] = useState<LogoUpload | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  // Kept small on purpose: these images ride along in every login response for every account that
  // has this panel linked, not fetched on demand -- a multi-megabyte photo would bloat every one
  // of those logins for a logo nobody needs at full resolution. mediaType: 'photo' already accepts
  // both jpg and png (client, 28 Oct: "it should admit both with and without background so it
  // should be JPG or PNG") -- the actual detected type travels with the picked asset.
  async function pickImage(onPicked: (upload: LogoUpload) => void) {
    setError(null);
    const result = await launchImageLibrary({
      mediaType: 'photo',
      includeBase64: true,
      maxWidth: 512,
      maxHeight: 512,
      quality: 0.8,
    });
    if (result.didCancel) return;
    const asset = result.assets?.[0];
    if (result.errorMessage || !asset?.base64) {
      setError(t('Could not read that image.'));
      return;
    }
    onPicked({ base64: asset.base64, mimeType: asset.type || 'image/jpeg' });
  }

  function validateCredentials(): string | null {
    if (!identifier.trim()) return t("Enter the panel's identifier.");
    if (!/^\d{4}$/.test(pin)) return t('The PIN must be 4 digits.');
    return null;
  }

  function reset() {
    setIdentifier('');
    setPin('');
    setLogo1(null);
    setLogo2(null);
  }

  // Both actions do the same thing around their one differing call: validate, run it, and pull the
  // account down again. That refresh is what makes the change visible now rather than at the next
  // login -- the logos travel inside the account payload, so without it the installer uploads an
  // image and nothing on screen changes ("the logos do not change" -- client, 30 Oct).
  async function run(action: () => Promise<{ State: ResponseState; Message?: string }>, okMessage: string, failMessage: string) {
    const validationError = validateCredentials();
    if (validationError) {
      setError(validationError);
      return;
    }
    setError(null);
    setNotice(null);
    setSubmitting(true);
    try {
      const res = await action();
      if (res.State === ResponseState.OK) {
        await refreshSession();
        setNotice(okMessage);
        reset();
        return;
      }
      setError(res.Message || failMessage);
    } catch {
      setError(t('Could not reach the server.'));
    } finally {
      setSubmitting(false);
    }
  }

  async function handleSubmit() {
    if (!logo1 && !logo2) {
      setError(t('Choose at least one logo first.'));
      return;
    }
    await run(
      () => setDeviceLogos(identifier.trim(), pin, logo1, logo2),
      t('Logos saved.'),
      t('Could not set the logos.'),
    );
  }

  async function handleReset() {
    await run(
      () => clearDeviceLogos(identifier.trim(), pin),
      t('Default images restored.'),
      t('Could not restore the default images.'),
    );
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
              {t("Enter the panel's identifier and its 4-digit user PIN, then choose the logos to show instead of the default Tausend look. You can change one or both.")}
            </Text>

            {error && <Banner kind="error">{error}</Banner>}
            {notice && <Banner kind="ok">{notice}</Banner>}

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
              {t('Logo 1 (portrait, splash screen)')}
            </Text>
            <PrimaryButton title={t('Choose image')} onPress={() => pickImage(setLogo1)} tone="outline" disabled={submitting} />
            {logo1 && (
              <View style={[styles.previewWrap, { marginTop: spacing.md }]}>
                <Image source={{ uri: logoDataUri(logo1.base64, logo1.mimeType) }} style={styles.previewPortrait} resizeMode="contain" />
              </View>
            )}

            <Text style={[typography.label, { color: colors.onDarkDim, marginTop: spacing.lg, marginBottom: spacing.xs }]}>
              {t('Logo 2 (landscape, home screen banner)')}
            </Text>
            <PrimaryButton title={t('Choose image')} onPress={() => pickImage(setLogo2)} tone="outline" disabled={submitting} />
            {logo2 && (
              <View style={[styles.previewWrap, { marginTop: spacing.md }]}>
                <Image source={{ uri: logoDataUri(logo2.base64, logo2.mimeType) }} style={styles.previewLandscape} resizeMode="contain" />
              </View>
            )}

            <View style={{ marginTop: spacing.lg }}>
              <PrimaryButton title={t('Save logos')} onPress={handleSubmit} loading={submitting} disabled={submitting} tone="dark" />
            </View>
            <View style={{ marginTop: spacing.sm }}>
              <PrimaryButton title={t('Reset to default images')} onPress={handleReset} disabled={submitting} tone="outline" />
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

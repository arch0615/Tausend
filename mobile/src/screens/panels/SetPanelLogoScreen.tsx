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
import { setCachedBrandLogo1 } from '../../branding/brandCache';

// Client, 26/27 Oct: sets the white-label logos for a panel by its identifier. Reached from
// Installer mode, which is itself behind the panel's installer code: "images can only be changed
// via the installer screen" (client, 30 Oct). Gated again here by the same live identifier+pin
// credential CreateDeviceScreen uses to pair a panel (DeviceBusiness.ValidatePinOnDevice) -- not
// an account-ownership check, since this can be done before any account is linked to the panel.
// Logo1 (portrait) is the splash screen; Logo2 (landscape) is the banner on that panel's own home
// screen. Either can be set on its own, and both can be dropped again with the reset button.
// ~1.5 MB of file, which base64 inflates by about a third. Comfortably fits a full-screen
// 1080x1920 PNG while still refusing a raw camera photo.
const MAX_LOGO_BASE64_LENGTH = 2_000_000;

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

  // quality 1 and no maxWidth/maxHeight deliberately: react-native-image-picker only skips its
  // decode-and-re-encode pass when quality is exactly 100 and no max dimension is set (see
  // Utils.shouldResizeImage). Any other setting re-encodes every pick, and that re-encode falls
  // back to JPEG unless the mime is exactly image/png, which flattens a transparent logo onto
  // black -- "even the logo 1 has no black background, it got the black background when uploaded"
  // (client, 2 Nov). Passing through the original bytes also keeps the full-screen splash sharp,
  // which a 512px cap could not.
  //
  // These images ride along in every login response for every account with this panel linked, so
  // the size ceiling that the resize used to provide is enforced here instead, with a message,
  // rather than silently shipping a multi-megabyte photo into every login.
  async function pickImage(onPicked: (upload: LogoUpload) => void) {
    setError(null);
    const result = await launchImageLibrary({
      mediaType: 'photo',
      includeBase64: true,
      quality: 1,
    });
    if (result.didCancel) return;
    const asset = result.assets?.[0];
    if (result.errorMessage || !asset?.base64) {
      setError(t('Could not read that image.'));
      return;
    }
    if (asset.base64.length > MAX_LOGO_BASE64_LENGTH) {
      setError(t('That image is too large. Choose one under 1.5 MB.'));
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
  async function run(
    action: () => Promise<{ State: ResponseState; Message?: string }>,
    okMessage: string,
    failMessage: string,
    afterOk?: () => Promise<void>,
  ) {
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
        await afterOk?.();
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
      // The splash reads the device's cached Logo1, which refreshSession otherwise refills from
      // whichever panel happens to come first in the account and carries one. With a stale test
      // logo on another panel that is not the one just uploaded, so the installer sees no change
      // ("a new Logo 1 can be uploaded but it doesn't work" -- client, 2 Nov). Writing it here
      // first makes what was just set win, and the refresh below cannot overwrite it with an
      // older panel's because resolveLogo1FromDevices now sees this panel's new value too.
      async () => {
        if (logo1) await setCachedBrandLogo1(logoDataUri(logo1.base64, logo1.mimeType));
      },
    );
  }

  async function handleReset() {
    await run(
      () => clearDeviceLogos(identifier.trim(), pin),
      t('Default images restored.'),
      t('Could not restore the default images.'),
      // Drop the remembered splash logo too, or the next launch still opens on the brand that
      // was just reset away.
      () => setCachedBrandLogo1(undefined),
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

import { useState } from 'react';
import { Alert, Image, KeyboardAvoidingView, Platform, ScrollView, StyleSheet, Text, View } from 'react-native';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import { useTheme } from '../../theme/ThemeProvider';
import { GradientBackground } from '../../components/GradientBackground';
import { Card } from '../../components/Card';
import { useLocale } from '../../i18n/LocaleContext';
import { useAuth } from '../../auth/AuthContext';
import { FormField } from '../../components/FormField';
import { PrimaryButton } from '../../components/PrimaryButton';
import { Banner } from '../../components/Banner';
import { LoadingOverlay } from '../../components/LoadingOverlay';
import type { AuthStackParamList } from '../../navigation/types';

// New APP Videos/'s login screen (the client's own reference for this app's design -- see
// memory/project_frontend_design_philosophy.md) puts the eagle mark + "ALARMAS TAUSEND" wordmark
// at the top of a white card floating on the gradient background, not as plain text directly on
// the gradient. Sourced from the project root's own logo.jpeg, with its solid red background
// keyed out to transparency (ffmpeg colorkey) so it sits directly on the card/gradient like the
// video shows, rather than as an opaque red square.
const EAGLE_LOGO = require('../../assets/images/eagle-logo.png');

type Props = NativeStackScreenProps<AuthStackParamList, 'Login'>;

export function LoginScreen({ route, navigation }: Props) {
  const { colors, typography, spacing } = useTheme();
  const { t } = useLocale();
  const { login } = useAuth();
  const [email, setEmail] = useState(route.params?.email ?? '');
  const [password, setPassword] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [notice] = useState(route.params?.notice ?? null);
  const [submitting, setSubmitting] = useState(false);
  // Bumped on every failed attempt to force the password TextInput to fully remount (see its key
  // prop below) -- client report: after one wrong password, even the correct one kept getting
  // rejected until the app was closed. Neither AuthContext.login() nor the backend's Login retain
  // any state across attempts (a failed login is a pure no-op on both), so this targets the other
  // explanation: Android autofill or the native input itself holding onto the rejected value
  // despite the controlled value/onChangeText looking right from the JS side.
  const [passwordFieldKey, setPasswordFieldKey] = useState(0);

  async function handleSubmit() {
    if (!email.trim() || !password) {
      setError(t('Enter your email and password.'));
      return;
    }
    setError(null);
    setSubmitting(true);
    try {
      const result = await login(email.trim(), password);
      if (!result.ok) {
        setError(result.message);
        setPassword('');
        setPasswordFieldKey((k) => k + 1);
        return;
      }
      // Ported from the previous app: the backend auto-unlinks an IP panel whose saved PIN no
      // longer validates against it, and login.page.ts named the affected panel(s) in a warning
      // dialog so the user knows to re-link rather than silently losing access.
      if (result.pinChanged && result.removedDevices.length > 0) {
        const names = result.removedDevices.map((d) => d.Description).join(', ');
        Alert.alert(
          t('Attention'),
          result.removedDevices.length === 1
            ? t('The PIN used for panel "{name}" is no longer valid. Please link it again.', { name: names })
            : t('The PIN used for panels {names} is no longer valid. Please link them again.', { names }),
        );
      }
      // On success, RootNavigator swaps to MainStack once the session updates --
      // nothing to navigate to here.
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
        <View style={styles.logoWrap}>
          <Image source={EAGLE_LOGO} style={styles.logo} resizeMode="contain" />
        </View>
        <Card style={styles.card}>
          <Text style={[styles.wordmark, { color: colors.brandBlue }]}>{t('ALARMAS')}</Text>
          <Text style={[styles.wordmark, styles.wordmarkSecondLine, { color: colors.brandRed }]}>{t('TAUSEND')}</Text>

          {notice && <Banner kind="ok">{notice}</Banner>}
          {error && <Banner kind="error">{error}</Banner>}

          <FormField
            label={t('Email')}
            value={email}
            onChangeText={setEmail}
            autoCapitalize="none"
            autoCorrect={false}
            keyboardType="email-address"
            textContentType="emailAddress"
            style={styles.input}
          />
          <FormField
            key={passwordFieldKey}
            label={t('Password')}
            value={password}
            onChangeText={setPassword}
            secureTextEntry
            textContentType="password"
            autoComplete="off"
            style={styles.input}
          />

          <View style={{ marginTop: spacing.sm }}>
            <PrimaryButton title={t('Acceder')} onPress={handleSubmit} disabled={submitting} />
          </View>
          <View style={{ marginTop: spacing.md }}>
            <PrimaryButton title={t('Registrarse')} onPress={() => navigation.navigate('Register')} tone="outline" />
          </View>

          <Text
            style={[typography.bodyDim, styles.forgotLink, { color: colors.accent, marginTop: spacing.lg }]}
            onPress={() => navigation.navigate('ForgotPassword')}
          >
            {t('Olvidé mi contraseña')}
          </Text>
        </Card>
      </ScrollView>
    </KeyboardAvoidingView>
    </GradientBackground>
    <LoadingOverlay visible={submitting} label={t('Loading')} />
    </>
  );
}

const styles = StyleSheet.create({
  container: {
    flexGrow: 1,
    justifyContent: 'center',
    padding: 24,
  },
  // The eagle sits above the card and slightly overlaps its top edge in New APP Videos/'s login,
  // not fully inside it -- a negative marginBottom on this wrapper pulls the card up underneath.
  // zIndex alone doesn't win on Android: Card's own `elevation` (see components/Card.tsx) makes
  // Android paint it above a merely-higher-zIndex sibling that has no elevation of its own, so the
  // card's white background was covering the eagle's lower half. elevation here has to beat
  // Card's (2) for the logo to actually stay on top.
  logoWrap: {
    alignItems: 'center',
    marginBottom: -28,
    zIndex: 10,
    elevation: 6,
  },
  logo: {
    width: 140,
    height: 140,
  },
  card: {
    alignItems: 'stretch',
    paddingTop: 40,
  },
  // Stacked on two lines ("ALARMAS" over "TAUSEND"), not side by side -- matches the wordmark's
  // actual layout in the reference video rather than one line with a space.
  wordmark: {
    fontSize: 30,
    fontWeight: '800',
    textAlign: 'center',
    lineHeight: 34,
  },
  wordmarkSecondLine: {
    marginBottom: 20,
  },
  // Pill-shaped, matching the reference's rounder login fields -- FormField's own default radius
  // is used everywhere else in the app and is deliberately left alone.
  input: {
    borderRadius: 24,
  },
  forgotLink: {
    textAlign: 'center',
    textDecorationLine: 'underline',
  },
});

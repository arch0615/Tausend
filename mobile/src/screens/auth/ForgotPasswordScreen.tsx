import { useState } from 'react';
import { KeyboardAvoidingView, Platform, ScrollView, StyleSheet, Text, View } from 'react-native';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import { useTheme } from '../../theme/ThemeProvider';
import { GradientBackground } from '../../components/GradientBackground';
import { useLocale } from '../../i18n/LocaleContext';
import { recoverPassword } from '../../api/auth';
import { FormField } from '../../components/FormField';
import { PrimaryButton } from '../../components/PrimaryButton';
import { Banner } from '../../components/Banner';
import type { AuthStackParamList } from '../../navigation/types';

type Props = NativeStackScreenProps<AuthStackParamList, 'ForgotPassword'>;

// The reset link itself completes on a web page (see backend PasswordResetUrl
// config), not in the app -- this screen only triggers that email.
export function ForgotPasswordScreen({ navigation }: Props) {
  const { colors, typography, spacing } = useTheme();
  const { t } = useLocale();
  const [email, setEmail] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [sent, setSent] = useState(false);
  const [submitting, setSubmitting] = useState(false);

  async function handleSubmit() {
    if (!email.trim()) {
      setError(t('Enter your email.'));
      return;
    }
    setError(null);
    setSubmitting(true);
    try {
      // The backend always reports success here, regardless of whether the
      // email is registered, so there's nothing to branch on -- show the same
      // confirmation either way.
      await recoverPassword(email.trim());
      setSent(true);
    } catch {
      setError(t('Could not reach the server.'));
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <GradientBackground style={{ flex: 1 }}>
    <KeyboardAvoidingView
      style={{ flex: 1 }}
      behavior={Platform.OS === 'ios' ? 'padding' : 'height'}
    >
      <ScrollView contentContainerStyle={styles.container} keyboardShouldPersistTaps="handled">
        <Text style={[typography.title, { color: colors.onDark, marginBottom: spacing.xs }]}>{t('Forgot password')}</Text>
        <Text style={[typography.bodyDim, { color: colors.onDarkDim, marginBottom: spacing.xl }]}>
          {t("We'll email you a link to reset it.")}
        </Text>

        {error && <Banner kind="error">{error}</Banner>}

        {sent ? (
          <Banner kind="ok">{t('If that email is registered, a reset link is on its way. Check your inbox.')}</Banner>
        ) : (
          <>
            <FormField labelColor={colors.onDarkDim}
              label={t('Email')}
              value={email}
              onChangeText={setEmail}
              autoCapitalize="none"
              autoCorrect={false}
              keyboardType="email-address"
              textContentType="emailAddress"
            />
            <View style={{ marginTop: spacing.sm }}>
              <PrimaryButton title={t('Send reset link')} onPress={handleSubmit} loading={submitting} />
            </View>
          </>
        )}

        <Text
          style={[typography.bodyDim, { color: colors.accent, marginTop: spacing.lg, textAlign: 'center' }]}
          onPress={() => navigation.goBack()}
        >
          {t('Back to login')}
        </Text>
      </ScrollView>
    </KeyboardAvoidingView>
    </GradientBackground>
  );
}

const styles = StyleSheet.create({
  container: {
    flexGrow: 1,
    justifyContent: 'center',
    padding: 24,
  },
});

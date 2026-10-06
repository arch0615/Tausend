import { useState } from 'react';
import { KeyboardAvoidingView, Platform, ScrollView, StyleSheet, Text, View } from 'react-native';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import { useTheme } from '../../theme/ThemeProvider';
import { GradientBackground } from '../../components/GradientBackground';
import { useLocale } from '../../i18n/LocaleContext';
import { createAccount } from '../../api/auth';
import { ResponseState } from '../../api/types';
import { FormField } from '../../components/FormField';
import { PrimaryButton } from '../../components/PrimaryButton';
import { Banner } from '../../components/Banner';
import { LoadingOverlay } from '../../components/LoadingOverlay';
import type { AuthStackParamList } from '../../navigation/types';

type Props = NativeStackScreenProps<AuthStackParamList, 'Register'>;

export function RegisterScreen({ navigation }: Props) {
  const { colors, typography, spacing } = useTheme();
  const { t } = useLocale();
  const [firstName, setFirstName] = useState('');
  const [lastName, setLastName] = useState('');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  async function handleSubmit() {
    if (!firstName.trim() || !lastName.trim() || !email.trim() || !password) {
      setError(t('Fill in every field.'));
      return;
    }
    if (password.length < 8) {
      setError(t('Password must be at least 8 characters.'));
      return;
    }
    setError(null);
    setSubmitting(true);
    try {
      const res = await createAccount(email.trim(), password, firstName.trim(), lastName.trim());
      if (res.State !== ResponseState.OK) {
        setError(res.Message || t('Could not create the account.'));
        return;
      }
      navigation.replace('Login', { email: email.trim(), notice: t('Account created. Log in to continue.') });
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
        <Text style={[typography.title, { color: colors.onDark, marginBottom: spacing.xs }]}>{t('Create account')}</Text>
        <Text style={[typography.bodyDim, { color: colors.onDarkDim, marginBottom: spacing.xl }]}>
          {t('Set up access to your alarm panels')}
        </Text>

        {error && <Banner kind="error">{error}</Banner>}

        <FormField labelColor={colors.onDarkDim} label={t('First name')} value={firstName} onChangeText={setFirstName} textContentType="givenName" />
        <FormField labelColor={colors.onDarkDim} label={t('Last name')} value={lastName} onChangeText={setLastName} textContentType="familyName" />
        <FormField labelColor={colors.onDarkDim}
          label={t('Email')}
          value={email}
          onChangeText={setEmail}
          autoCapitalize="none"
          autoCorrect={false}
          keyboardType="email-address"
          textContentType="emailAddress"
        />
        <FormField labelColor={colors.onDarkDim}
          label={t('Password')}
          value={password}
          onChangeText={setPassword}
          secureTextEntry
          textContentType="newPassword"
        />

        <View style={{ marginTop: spacing.sm }}>
          <PrimaryButton title={t('Create account')} onPress={handleSubmit} disabled={submitting} />
        </View>

        <Text
          style={[typography.bodyDim, { color: colors.accent, marginTop: spacing.lg, textAlign: 'center' }]}
          onPress={() => navigation.goBack()}
        >
          {t('Back to login')}
        </Text>
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
});

import { useState } from 'react';
import { Alert, KeyboardAvoidingView, Platform, ScrollView, StyleSheet, Text, View } from 'react-native';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import { useTheme } from '../../theme/ThemeProvider';
import { GradientBackground } from '../../components/GradientBackground';
import { useLocale } from '../../i18n/LocaleContext';
import { useAuth } from '../../auth/AuthContext';
import { updateAccountPassword } from '../../api/auth';
import { ResponseState } from '../../api/types';
import { describeCommandException, describeCommandFailure } from '../../panels/commandFeedback';
import { FormField } from '../../components/FormField';
import { PrimaryButton } from '../../components/PrimaryButton';
import { Banner } from '../../components/Banner';
import type { MainStackParamList } from '../../navigation/types';

type Props = NativeStackScreenProps<MainStackParamList, 'ChangePassword'>;

export function ChangePasswordScreen(_props: Props) {
  const { colors, typography, spacing } = useTheme();
  const { t } = useLocale();
  const { logout } = useAuth();
  const [oldPassword, setOldPassword] = useState('');
  const [newPassword, setNewPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  function handleSubmit() {
    if (!oldPassword || !newPassword) {
      setError(t('Fill in every field.'));
      return;
    }
    if (newPassword.length < 8) {
      setError(t('New password must be at least 8 characters.'));
      return;
    }
    if (newPassword !== confirmPassword) {
      setError(t('New passwords do not match.'));
      return;
    }
    if (newPassword === oldPassword) {
      setError(t('New password must be different from your current password.'));
      return;
    }
    setError(null);
    // Ported from the previous app: changing the password invalidates the current session
    // server-side, so the user is warned up front and logged out on success below.
    Alert.alert(t('Change password?'), t("You'll be logged out automatically afterward."), [
      { text: t('Cancel'), style: 'cancel' },
      { text: t('Continue'), onPress: submit },
    ]);
  }

  async function submit() {
    setSubmitting(true);
    try {
      const res = await updateAccountPassword(oldPassword, newPassword);
      if (res.State === ResponseState.UNAUTHORIZED) {
        // No manual navigation needed -- RootNavigator swaps to the login stack as soon as
        // session clears (see navigation/RootNavigator.tsx).
        logout();
        return;
      }
      if (res.State !== ResponseState.OK) {
        setError(describeCommandFailure(res, t));
        return;
      }
      logout();
    } catch (e) {
      setError(describeCommandException(e, t));
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
        <Text style={[typography.title, { color: colors.onDark, marginBottom: spacing.xl }]}>{t('Change password')}</Text>

        {error && <Banner kind="error">{error}</Banner>}

        <FormField labelColor={colors.onDarkDim}
          label={t('Current password')}
          value={oldPassword}
          onChangeText={setOldPassword}
          secureTextEntry
          textContentType="password"
        />
        <FormField labelColor={colors.onDarkDim}
          label={t('New password')}
          value={newPassword}
          onChangeText={setNewPassword}
          secureTextEntry
          textContentType="newPassword"
        />
        <FormField labelColor={colors.onDarkDim}
          label={t('Confirm new password')}
          value={confirmPassword}
          onChangeText={setConfirmPassword}
          secureTextEntry
          textContentType="newPassword"
        />

        <View style={{ marginTop: spacing.sm }}>
          <PrimaryButton title={t('Save')} onPress={handleSubmit} loading={submitting} />
        </View>
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

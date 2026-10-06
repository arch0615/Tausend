import { useState } from 'react';
import { Alert, ScrollView, StyleSheet, Text, View } from 'react-native';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import { useTheme } from '../../theme/ThemeProvider';
import { GradientBackground } from '../../components/GradientBackground';
import { Card } from '../../components/Card';
import { useLocale } from '../../i18n/LocaleContext';
import { useAuth } from '../../auth/AuthContext';
import { deleteAccount } from '../../api/auth';
import { ResponseState } from '../../api/types';
import { describeCommandException, describeCommandFailure } from '../../panels/commandFeedback';
import { FormField } from '../../components/FormField';
import { PrimaryButton } from '../../components/PrimaryButton';
import { Banner } from '../../components/Banner';
import type { MainStackParamList } from '../../navigation/types';

type Props = NativeStackScreenProps<MainStackParamList, 'UserAccount'>;

// The client's own "Pruebas 25-9-26" feedback pointed at the previous app's Ajustes -> "Cuenta
// de usuario" screen (Nombre/Apellido/Email + "Eliminar Cuenta") -- account.page.ts -- which never
// made it into this rebuild even though the backend's AccountService/DeleteAccount endpoint
// (a soft delete, see DeleteAccount.sql) was already fully built and wired.
export function UserAccountScreen({ navigation }: Props) {
  const { colors, typography, spacing } = useTheme();
  const { t } = useLocale();
  const { session, logout } = useAuth();
  const [error, setError] = useState<string | null>(null);
  const [deleting, setDeleting] = useState(false);

  function confirmDelete() {
    Alert.alert(
      t('Delete account?'),
      t('This permanently removes your account and unlinks your panel. This cannot be undone.'),
      [
        { text: t('Cancel'), style: 'cancel' },
        { text: t('Delete account'), style: 'destructive', onPress: submitDelete },
      ],
    );
  }

  async function submitDelete() {
    setError(null);
    setDeleting(true);
    try {
      const res = await deleteAccount();
      if (res.State !== ResponseState.OK && res.State !== ResponseState.UNAUTHORIZED) {
        setError(describeCommandFailure(res, t));
        return;
      }
      // No manual navigation needed -- RootNavigator swaps to the login stack as soon as
      // session clears (see navigation/RootNavigator.tsx).
      logout();
    } catch (e) {
      setError(describeCommandException(e, t));
    } finally {
      setDeleting(false);
    }
  }

  return (
    <GradientBackground style={{ flex: 1 }}>
      <ScrollView contentContainerStyle={styles.container}>
        <Text style={[typography.title, { color: colors.onDark, marginBottom: spacing.xl }]}>{t('User account')}</Text>

        {error && <Banner kind="error">{error}</Banner>}

        <Card style={styles.card}>
          <FormField label={t('First name')} value={session?.account.FirstName ?? ''} editable={false} style={styles.readOnlyField} />
          <FormField label={t('Last name')} value={session?.account.LastName ?? ''} editable={false} style={styles.readOnlyField} />
          <FormField label={t('Email')} value={session?.account.Email ?? ''} editable={false} style={styles.readOnlyField} />
        </Card>

        <View style={{ marginTop: spacing.xl }}>
          <PrimaryButton title={t('Delete account')} onPress={confirmDelete} loading={deleting} tone="danger" />
        </View>
        <View style={{ marginTop: spacing.sm }}>
          <PrimaryButton title={t('Cancel')} onPress={() => navigation.goBack()} tone="outline" disabled={deleting} />
        </View>
      </ScrollView>
    </GradientBackground>
  );
}

const styles = StyleSheet.create({
  container: {
    flexGrow: 1,
    padding: 24,
  },
  card: {
    alignItems: 'stretch',
  },
  // Read-only display -- matches the reference's plain underlined, non-editable presentation
  // rather than FormField's usual bordered/editable box.
  readOnlyField: {
    borderWidth: 0,
    borderBottomWidth: 1,
    borderRadius: 0,
    paddingHorizontal: 0,
  },
});

import { ScrollView, StyleSheet, Text } from 'react-native';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import { useTheme } from '../../theme/ThemeProvider';
import { GradientBackground } from '../../components/GradientBackground';
import { useLocale } from '../../i18n/LocaleContext';
import { useAuth } from '../../auth/AuthContext';
import { PrimaryButton } from '../../components/PrimaryButton';
import { Banner } from '../../components/Banner';
import type { MainStackParamList } from '../../navigation/types';

type Props = NativeStackScreenProps<MainStackParamList, 'PairingConfirmation'>;

export function PairingConfirmationScreen({ route }: Props) {
  const { colors, typography, spacing } = useTheme();
  const { t } = useLocale();
  const { logout } = useAuth();
  const { path } = route.params;

  return (
    <GradientBackground style={{ flex: 1 }}>
    <ScrollView contentContainerStyle={styles.container}>
      <Text style={[typography.title, { color: colors.onDark, marginBottom: spacing.md }]}>
        {t('Panel added')}
      </Text>
      <Banner kind="ok">
        {path === 'wifi' ? t('Your panel has been linked to your account.') : t('Your SMS panel has been registered.')}
      </Banner>
      <Text style={[typography.body, { color: colors.onDark, marginTop: spacing.md, marginBottom: spacing.xl }]}>
        {path === 'wifi'
          ? t("It should come online within a minute or two. If it doesn't, put it back into setup mode and double-check the Wi-Fi name and password you gave it.")
          : t("You can now control it by text message. Commands are sent from your phone's own messaging app, one confirmation tap at a time.")}
      </Text>
      <Text style={[typography.bodyDim, { color: colors.onDarkDim, marginBottom: spacing.lg }]}>
        {t("For security, you'll be logged out now -- log back in to see your new panel.")}
      </Text>
      {/* Ported from the previous app: linking a panel always force-logged-out the session
          afterward (a fresh login re-fetches the account's full, now-updated device list). */}
      <PrimaryButton title={t('Done')} onPress={() => logout()} />
    </ScrollView>
    </GradientBackground>
  );
}

const styles = StyleSheet.create({
  container: {
    flexGrow: 1,
    padding: 24,
    justifyContent: 'center',
  },
});

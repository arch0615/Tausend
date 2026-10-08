import { ScrollView, StyleSheet, Text, View } from 'react-native';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import { useTheme } from '../../theme/ThemeProvider';
import { GradientBackground } from '../../components/GradientBackground';
import { useLocale } from '../../i18n/LocaleContext';
import { PrimaryButton } from '../../components/PrimaryButton';
import type { MainStackParamList } from '../../navigation/types';

type Props = NativeStackScreenProps<MainStackParamList, 'SelectPairingType'>;

export function SelectPairingTypeScreen({ navigation }: Props) {
  const { colors, typography, spacing } = useTheme();
  const { t } = useLocale();

  return (
    <GradientBackground style={{ flex: 1 }}>
    <ScrollView contentContainerStyle={styles.container}>
      <Text style={[typography.title, { color: colors.onDark, marginBottom: spacing.md }]}>{t('Add a panel')}</Text>
      <Text style={[typography.bodyDim, { color: colors.onDarkDim, marginBottom: spacing.xl }]}>
        {t('How does your panel connect?')}
      </Text>

      {/* "Añadir Equipo" registers an already-networked panel by its identifier+PIN
          (CreateDeviceScreen) -- distinct from "Conectar Access Point" (PairingIntro), the
          separate Wi-Fi setup wizard for a panel that isn't on the home network yet at all.
          Ported from setup.page.ts's handleCreate('ip'), which reveals the create form directly
          with no Wi-Fi step involved; this used to route into that wizard by mistake, leaving no
          way to actually register a panel once it already had Wi-Fi configured. */}
      <PrimaryButton title={t('IP panel')} onPress={() => navigation.navigate('CreateDevice')} />
      <View style={{ height: spacing.md }} />
      <PrimaryButton title={t('SMS panel')} onPress={() => navigation.navigate('SmsPairing')} />

      <Text style={[typography.bodyDim, { color: colors.onDarkDim, marginTop: spacing.xl }]}>
        {t(
          "IP panels connect to the app's server through your home Wi-Fi network and/or the mobile data of the alarm's cellular line. SMS panels do not use a server connection and are controlled by text messages between the users' phones and the alarm's cellular line.",
        )}
      </Text>
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

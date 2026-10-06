import { ScrollView, StyleSheet, Text, View } from 'react-native';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import { useTheme } from '../../theme/ThemeProvider';
import { GradientBackground } from '../../components/GradientBackground';
import { useLocale } from '../../i18n/LocaleContext';
import { PrimaryButton } from '../../components/PrimaryButton';
import type { MainStackParamList } from '../../navigation/types';

type Props = NativeStackScreenProps<MainStackParamList, 'PairingIntro'>;

export function PairingIntroScreen({ navigation }: Props) {
  const { colors, typography, spacing } = useTheme();
  const { t } = useLocale();

  return (
    <GradientBackground style={{ flex: 1 }}>
    <ScrollView contentContainerStyle={styles.container}>
      <Text style={[typography.title, { color: colors.onDark, marginBottom: spacing.md }]}>{t('Access Point')}</Text>
      <Text style={[typography.body, { color: colors.onDark, marginBottom: spacing.sm }]}>
        {t(
          "Before you start, put your alarm panel into Access Point (setup) mode -- check the panel's manual for the exact button or installer-code sequence for your model.",
        )}
      </Text>
      <Text style={[typography.bodyDim, { color: colors.onDarkDim, marginBottom: spacing.xl }]}>
        {t(
          // Not just for adding a new panel -- client's "Access Point" feedback: this same entry
          // point also reaches an already-linked panel's Access Point to check or change its
          // programming (e.g. via the raw COMMAND console on ConnectToPanelScreen), so the copy
          // shouldn't assume Wi-Fi setup is the only reason to be here.
          "Once it's in setup mode, the panel will broadcast its own Wi-Fi network. You'll connect to that network briefly to check and/or modify its programming.",
        )}
      </Text>
      <View style={{ marginTop: spacing.md }}>
        <PrimaryButton title={t("I'm ready")} onPress={() => navigation.navigate('ConnectToPanel')} />
      </View>
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

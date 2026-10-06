import { Pressable, StyleSheet, Text } from 'react-native';
import { useTheme } from '../../theme/ThemeProvider';
import { GradientBackground } from '../../components/GradientBackground';
import { useLocale, type Locale } from '../../i18n/LocaleContext';

const OPTIONS: { locale: Locale; label: string }[] = [
  { locale: 'en', label: 'English' },
  { locale: 'es', label: 'Español' },
];

export function LanguageScreen() {
  const { colors, typography, spacing, radius } = useTheme();
  const { locale, setLocale, t } = useLocale();

  return (
    <GradientBackground style={styles.container}>
      <Text style={[typography.bodyDim, { color: colors.onDarkDim, marginBottom: spacing.lg }]}>
        {t('Choose the language for this app.')}
      </Text>
      {OPTIONS.map((option) => {
        const isSelected = locale === option.locale;
        return (
          <Pressable
            key={option.locale}
            onPress={() => setLocale(option.locale)}
            style={[
              styles.row,
              {
                backgroundColor: colors.panel,
                borderColor: isSelected ? colors.accent : colors.line,
                borderRadius: radius.md,
                marginBottom: spacing.sm,
              },
            ]}
          >
            <Text style={[typography.body, { color: colors.ink }]}>{option.label}</Text>
            {isSelected && <Text style={[typography.body, { color: colors.accent }]}>✓</Text>}
          </Pressable>
        );
      })}
    </GradientBackground>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    padding: 24,
  },
  row: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    padding: 16,
    borderWidth: 1,
  },
});

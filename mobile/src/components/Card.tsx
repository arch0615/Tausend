import { StyleSheet, View, type ViewProps } from 'react-native';
import { useTheme } from '../theme/ThemeProvider';

interface CardProps extends ViewProps {
  /** 'default' is a neutral panel surface; 'hero' is the deep-accent-fill treatment used for
   * the single most important piece of state on a screen (e.g. Home's panel status). */
  tone?: 'default' | 'hero';
}

export function Card({ tone = 'default', style, children, ...rest }: CardProps) {
  const { colors, radius, spacing } = useTheme();
  const hero = tone === 'hero';
  return (
    <View
      style={[
        styles.card,
        {
          backgroundColor: hero ? colors.accent : colors.panel,
          borderColor: hero ? colors.accent : colors.line,
          borderRadius: radius.lg,
          padding: spacing.lg,
          shadowColor: hero ? colors.accent : '#000',
          shadowOpacity: hero ? 0.3 : 0.06,
        },
        style,
      ]}
      {...rest}
    >
      {children}
    </View>
  );
}

const styles = StyleSheet.create({
  card: {
    borderWidth: 1,
    shadowOffset: { width: 0, height: 8 },
    shadowRadius: 20,
    elevation: 2,
  },
});

import { StyleSheet, Text } from 'react-native';
import { useTheme } from '../theme/ThemeProvider';

interface BannerProps {
  kind: 'error' | 'ok';
  children: string;
}

export function Banner({ kind, children }: BannerProps) {
  const { colors, typography, spacing, radius } = useTheme();
  const isError = kind === 'error';
  return (
    <Text
      style={[
        styles.banner,
        typography.bodyDim,
        {
          color: isError ? colors.danger : colors.okInk,
          backgroundColor: isError ? colors.dangerBg : colors.okBg,
          borderRadius: radius.sm,
          padding: spacing.md,
          marginBottom: spacing.md,
        },
      ]}
    >
      {children}
    </Text>
  );
}

const styles = StyleSheet.create({
  banner: {
    overflow: 'hidden',
  },
});

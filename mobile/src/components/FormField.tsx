import { useState } from 'react';
import { Pressable, StyleSheet, Text, TextInput, View, type TextInputProps } from 'react-native';
import { useTheme } from '../theme/ThemeProvider';
import { useLocale } from '../i18n/LocaleContext';

interface FormFieldProps extends TextInputProps {
  label: string;
  // inkDim is tuned for text on white cards -- screens that render fields directly on the dark
  // gradient (no Card wrapper) need to pass colors.onDarkDim instead, or this label is nearly
  // invisible. Defaults to inkDim since most fields do sit on a white card/panel.
  labelColor?: string;
}

export function FormField({ label, style, secureTextEntry, labelColor, ...inputProps }: FormFieldProps) {
  const { colors, typography, spacing, radius } = useTheme();
  const { t } = useLocale();
  // The previous app had an eye-icon toggle on every password field; this app had none at all,
  // leaving no way to check what you just typed before submitting.
  const [visible, setVisible] = useState(false);

  return (
    <View style={{ marginBottom: spacing.md }}>
      <Text style={[typography.label, { color: labelColor ?? colors.inkDim, marginBottom: spacing.xs }]}>{label}</Text>
      <View style={styles.inputRow}>
        <TextInput
          placeholderTextColor={colors.inkDim}
          secureTextEntry={secureTextEntry && !visible}
          style={[
            styles.input,
            {
              flex: 1,
              color: colors.ink,
              backgroundColor: colors.panel,
              borderColor: colors.line,
              borderRadius: radius.sm,
            },
            style,
          ]}
          {...inputProps}
        />
        {secureTextEntry && (
          <Pressable onPress={() => setVisible((v) => !v)} style={styles.toggle} hitSlop={8}>
            <Text style={[typography.label, { color: colors.accent }]}>{visible ? t('Hide') : t('Show')}</Text>
          </Pressable>
        )}
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  inputRow: {
    flexDirection: 'row',
    alignItems: 'center',
  },
  input: {
    borderWidth: 1,
    paddingHorizontal: 12,
    paddingVertical: 10,
    fontSize: 15,
  },
  toggle: {
    marginLeft: 10,
  },
});

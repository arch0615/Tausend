import { ActivityIndicator, Pressable, StyleSheet, Text, View } from 'react-native';
import { useTheme } from '../theme/ThemeProvider';

interface PrimaryButtonProps {
  title: string;
  onPress: () => void;
  disabled?: boolean;
  loading?: boolean;
  // 'danger' matches New APP Videos/'s coral-red treatment for panic/duress/emergency/disarm --
  // everywhere else defaults to the teal-green primary action color. 'outline' matches the
  // video's secondary-action style (e.g. login's "Registrarse" below "Acceder") -- a bordered,
  // unfilled button rather than a second solid color competing with the primary action. 'dark'
  // matches the Access Point flow's own in-progress actions (Añadir Equipo, Conectar, Configurar
  // nombre de red y contraseña) -- that flow reserves the green accent specifically for
  // "finished/go home" actions (Conectar Access Point, Finalizar, Inicio).
  tone?: 'accent' | 'danger' | 'outline' | 'dark';
}

export function PrimaryButton({ title, onPress, disabled, loading, tone = 'accent' }: PrimaryButtonProps) {
  const { colors, typography, spacing, radius } = useTheme();
  const isDisabled = disabled || loading;
  const outline = tone === 'outline';
  const fill = tone === 'danger' ? colors.danger : tone === 'dark' ? colors.headerBg : colors.accent;
  // The Pressable is a bare wrapper and every visual (fill, border, padding, elevation, shadow)
  // lives on the View inside it -- the same shape ListRow already uses.
  //
  // These used to all sit on the Pressable itself with just a Text inside, and on Android that
  // left the button's padding dead to touch: only the text's own band responded, so tapping the
  // coloured area a few millimetres above or below the label did nothing. Measured on a device,
  // the button is 127px tall but only the middle 54px (exactly the text's height) reacted.
  // Wrapping the styled View instead gives the Pressable a child that fills it, so the whole
  // button is hit-testable.
  //
  // hitSlop stays on top of that as a cheap guarantee, and widens the target past the visual
  // edge, which is what the platform touch-target guidance wants anyway.
  return (
    <Pressable
      onPress={onPress}
      disabled={isDisabled}
      hitSlop={8}
      style={{ opacity: isDisabled ? 0.6 : 1 }}
    >
      <View
        style={[
          styles.button,
          {
            backgroundColor: outline ? 'transparent' : fill,
            borderWidth: outline ? 1.5 : 0,
            borderColor: outline ? colors.accent : 'transparent',
            borderRadius: radius.sm,
            paddingVertical: spacing.md + 2,
            shadowColor: fill,
            shadowOpacity: outline ? 0 : styles.button.shadowOpacity,
            elevation: outline ? 0 : styles.button.elevation,
          },
        ]}
      >
        {loading ? (
          <ActivityIndicator color={outline ? colors.accent : colors.accentInk} />
        ) : (
          <Text style={[typography.button, { color: outline ? colors.accent : colors.accentInk }]}>{title}</Text>
        )}
      </View>
    </Pressable>
  );
}

const styles = StyleSheet.create({
  button: {
    alignItems: 'center',
    justifyContent: 'center',
    shadowOffset: { width: 0, height: 6 },
    shadowOpacity: 0.28,
    shadowRadius: 12,
    elevation: 3,
  },
});

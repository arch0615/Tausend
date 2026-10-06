import { View } from 'react-native';
import { useTheme } from '../theme/ThemeProvider';

interface StatusDotProps {
  color: string;
  size?: number;
  glow?: boolean;
}

// The LED-indicator language used throughout the panel-control screens (matches a physical
// keypad's own status lights) -- a small filled dot, optionally with a soft halo so it reads
// as "lit" rather than just a colored circle.
export function StatusDot({ color, size = 8, glow = true }: StatusDotProps) {
  const { dark } = useTheme();
  return (
    <View
      style={{
        width: size,
        height: size,
        borderRadius: size / 2,
        backgroundColor: color,
        shadowColor: color,
        shadowOffset: { width: 0, height: 0 },
        shadowOpacity: glow ? (dark ? 0.9 : 0.5) : 0,
        shadowRadius: glow ? size : 0,
        elevation: glow ? 2 : 0,
      }}
    />
  );
}

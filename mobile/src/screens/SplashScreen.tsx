import { useEffect, useState } from 'react';
import { ActivityIndicator, Image, StyleSheet, View } from 'react-native';
import { useTheme } from '../theme/ThemeProvider';
import { GradientBackground } from '../components/GradientBackground';
import { getCachedBrandLogo1 } from '../branding/brandCache';

const LOGO1_DEFAULT = require('../assets/images/logo1-default.png');

/**
 * Shown while the app opens: "Logo 1 should be displayed as a splash screen when the app is
 * opened. Currently, the app icon is shown; please replace it with Logo 1" (client, 30 Oct).
 *
 * Which Logo1 is a per-account question, but nothing about the account is known this early, so it
 * uses the one remembered from the last successful login on this device (brandCache) and falls
 * back to the bundled logo1.png. That is also why this is a JS screen rather than the Android
 * splash theme -- the native one is baked into the APK and cannot vary per reseller.
 */
export function SplashScreen() {
  const { colors } = useTheme();
  const [logo, setLogo] = useState<string | null>(null);
  // Rendering before the cache read resolves would show the default for a frame and then swap,
  // which reads as a glitch on exactly the screen meant to look deliberate.
  const [cacheRead, setCacheRead] = useState(false);

  useEffect(() => {
    let active = true;
    getCachedBrandLogo1().then((cached) => {
      if (!active) return;
      setLogo(cached);
      setCacheRead(true);
    });
    return () => {
      active = false;
    };
  }, []);

  return (
    <GradientBackground style={styles.root}>
      {cacheRead && (
        <Image
          source={logo ? { uri: logo } : LOGO1_DEFAULT}
          style={styles.logo}
          resizeMode="contain"
        />
      )}
      <View style={styles.spinner}>
        <ActivityIndicator color={colors.accent} />
      </View>
    </GradientBackground>
  );
}

const styles = StyleSheet.create({
  root: {
    flex: 1,
    alignItems: 'center',
    justifyContent: 'center',
  },
  logo: {
    width: '62%',
    height: '45%',
  },
  spinner: {
    position: 'absolute',
    bottom: 64,
  },
});

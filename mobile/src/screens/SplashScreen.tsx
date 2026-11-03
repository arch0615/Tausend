import { Image, StyleSheet } from 'react-native';
import { GradientBackground } from '../components/GradientBackground';

const LOGO1_DEFAULT = require('../assets/images/logo1-default.png');

/**
 * The opening screen: "Logo 1 should be displayed as a splash screen when the app is opened"
 * (client, 30 Oct), full screen, with the client's own 1080x1920 logo1.png as the default.
 *
 * Whether this is shown at all is RootNavigator's call, not this screen's -- it is held back on a
 * very first launch, and it needs the cached logo resolved before it can decide, so the logo
 * arrives as a prop rather than being read here.
 *
 * resizeMode is contain rather than cover: the art is 9:16 and phones are taller than that, so
 * cover would crop the eagle's wings. Contained, the gradient simply continues above and below.
 */
export function SplashScreen({ logo }: { logo?: string }) {
  return (
    <GradientBackground style={styles.root}>
      <Image source={logo ? { uri: logo } : LOGO1_DEFAULT} style={styles.logo} resizeMode="contain" />
    </GradientBackground>
  );
}

const styles = StyleSheet.create({
  root: {
    flex: 1,
  },
  logo: {
    width: '100%',
    height: '100%',
  },
});

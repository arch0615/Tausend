import { useEffect, useState } from 'react';
import { NavigationContainer, DefaultTheme, DarkTheme, type Theme } from '@react-navigation/native';
import { useTheme } from '../theme/ThemeProvider';
import { SplashScreen } from '../screens/SplashScreen';
import { useAuth } from '../auth/AuthContext';
import { handleInitialNotification, subscribeToNotificationOpen } from '../notifications/messaging';
import { navigationRef } from './navigationRef';
import { AuthStack } from './AuthStack';
import { MainStack } from './MainStack';

function navigationTheme(colors: ReturnType<typeof useTheme>['colors'], dark: boolean): Theme {
  const base = dark ? DarkTheme : DefaultTheme;
  return {
    ...base,
    dark,
    colors: {
      ...base.colors,
      primary: colors.accent,
      background: colors.bg,
      card: colors.headerBg,
      text: colors.headerInk,
      border: colors.line,
    },
  };
}

// Auth usually resolves in well under a second, and a logo that appears for three frames is not a
// splash screen -- it reads as a flicker. Holding it for this long makes it the deliberate opening
// the client asked for without being long enough to feel like a stall.
const MIN_SPLASH_MS = 1500;

export function RootNavigator() {
  const theme = useTheme();
  const { session, isLoading } = useAuth();
  const [minSplashElapsed, setMinSplashElapsed] = useState(false);

  useEffect(() => {
    const timer = setTimeout(() => setMinSplashElapsed(true), MIN_SPLASH_MS);
    return () => clearTimeout(timer);
  }, []);

  // isLoading covers the real work (keychain lookup + refresh-token round trip in AuthContext).
  if (isLoading || !minSplashElapsed) {
    return <SplashScreen />;
  }

  return (
    <NavigationContainer
      ref={navigationRef}
      theme={navigationTheme(theme.colors, theme.dark)}
      onReady={() => {
        // Covers the "app was killed, opened by tapping a notification" case -- the
        // foreground/background tap case is handled by NotificationOpenSubscription below,
        // which only needs to exist once the container (and this callback) has already run.
        handleInitialNotification();
      }}
    >
      <NotificationOpenSubscription />
      {session ? <MainStack /> : <AuthStack />}
    </NavigationContainer>
  );
}

function NotificationOpenSubscription() {
  useEffect(() => subscribeToNotificationOpen(), []);
  return null;
}

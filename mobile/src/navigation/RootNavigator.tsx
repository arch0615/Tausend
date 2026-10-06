import { useEffect } from 'react';
import { ActivityIndicator, View } from 'react-native';
import { NavigationContainer, DefaultTheme, DarkTheme, type Theme } from '@react-navigation/native';
import { useTheme } from '../theme/ThemeProvider';
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

export function RootNavigator() {
  const theme = useTheme();
  const { session, isLoading } = useAuth();

  if (isLoading) {
    // Keychain lookup + refresh-token round trip on launch (AuthContext) -- brief,
    // but real, so a blank flash would look like a bug.
    return (
      <View style={{ flex: 1, alignItems: 'center', justifyContent: 'center', backgroundColor: theme.colors.bg }}>
        <ActivityIndicator color={theme.colors.accent} />
      </View>
    );
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

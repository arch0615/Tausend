/**
 * Tausend mobile app
 *
 * @format
 */

import { StatusBar, useColorScheme } from 'react-native';
import { SafeAreaProvider } from 'react-native-safe-area-context';
import { ThemeProvider } from './src/theme/ThemeProvider';
import { LocaleProvider } from './src/i18n/LocaleContext';
import { AuthProvider } from './src/auth/AuthContext';
import { PanelProvider } from './src/panels/PanelContext';
import { RootNavigator } from './src/navigation/RootNavigator';

function App() {
  const isDarkMode = useColorScheme() === 'dark';

  return (
    <SafeAreaProvider>
      <ThemeProvider>
        <LocaleProvider>
          <AuthProvider>
            <PanelProvider>
              <StatusBar barStyle={isDarkMode ? 'light-content' : 'dark-content'} />
              <RootNavigator />
            </PanelProvider>
          </AuthProvider>
        </LocaleProvider>
      </ThemeProvider>
    </SafeAreaProvider>
  );
}

export default App;

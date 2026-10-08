import { useState } from 'react';
import { createNativeStackNavigator } from '@react-navigation/native-stack';
import { useTheme } from '../theme/ThemeProvider';
import { useLocale } from '../i18n/LocaleContext';
import { MainMenu, MenuButton } from '../components/MainMenu';
import { HomeScreen } from '../screens/main/HomeScreen';
import { ChangePasswordScreen } from '../screens/main/ChangePasswordScreen';
import { SelectPairingTypeScreen } from '../screens/pairing/SelectPairingTypeScreen';
import { PairingIntroScreen } from '../screens/pairing/PairingIntroScreen';
import { ConnectToPanelScreen } from '../screens/pairing/ConnectToPanelScreen';
import { CreateDeviceScreen } from '../screens/pairing/CreateDeviceScreen';
import { SmsPairingScreen } from '../screens/pairing/SmsPairingScreen';
import { PairingConfirmationScreen } from '../screens/pairing/PairingConfirmationScreen';
import { PanelSelectorScreen } from '../screens/panels/PanelSelectorScreen';
import { ManagePanelScreen } from '../screens/panels/ManagePanelScreen';
import { PanelIdentificationScreen } from '../screens/panels/PanelIdentificationScreen';
import { ZonesScreen } from '../screens/panels/ZonesScreen';
import { ExclusionsScreen } from '../screens/panels/ExclusionsScreen';
import { MemoryScreen } from '../screens/panels/MemoryScreen';
import { PgmScreen } from '../screens/panels/PgmScreen';
import { ScheduledDeparturesScreen } from '../screens/panels/ScheduledDeparturesScreen';
import { PanelUsersScreen } from '../screens/panels/PanelUsersScreen';
import { BatteryScreen } from '../screens/panels/BatteryScreen';
import { FailuresScreen } from '../screens/panels/FailuresScreen';
import { EventsScreen } from '../screens/panels/EventsScreen';
import { CustomMessagesScreen } from '../screens/panels/CustomMessagesScreen';
import { ContactPhoneScreen } from '../screens/panels/ContactPhoneScreen';
import { SmsPasswordChangeScreen } from '../screens/panels/SmsPasswordChangeScreen';
import { ClockScreen } from '../screens/panels/ClockScreen';
import { InstallerModeScreen } from '../screens/panels/InstallerModeScreen';
import { LanguageScreen } from '../screens/main/LanguageScreen';
import { UserAccountScreen } from '../screens/main/UserAccountScreen';
import type { MainStackParamList } from './types';

const Stack = createNativeStackNavigator<MainStackParamList>();

export function MainStack() {
  const { colors } = useTheme();
  const { t } = useLocale();
  const [menuOpen, setMenuOpen] = useState(false);
  return (
    <>
    <Stack.Navigator
      // Reset on every screen focus. This menu's Modal is a sibling of the navigator, so a
      // dismissal that doesn't round-trip through onClose (the modal window being torn down
      // underneath a native-stack transition, which is what MainMenu.go does: it calls onClose
      // and navigates in the same tick) left menuOpen stuck at true with nothing on screen.
      // The header button then called setMenuOpen(true) on an already-true state, which is not
      // a state change, so nothing re-rendered and the button looked permanently dead on every
      // stack screen -- while Home kept working because HomeScreen holds its own separate
      // menuOpen. That is the client's point 1 ("only the back arrow responds", "always when it
      // was on the right side": the right-hand header button is this one, Home's is its own).
      screenListeners={{ focus: () => setMenuOpen(false) }}
      screenOptions={{
        headerStyle: { backgroundColor: colors.headerBg },
        headerTintColor: colors.headerInk,
        headerTitleStyle: { fontWeight: '700' },
        headerShadowVisible: true,
        contentStyle: { backgroundColor: colors.bg },
        // Present on every screen, matching the previous app's persistent side-drawer menu --
        // every section is one tap away from anywhere, not just from Home. Kept on the right
        // (legacy's was top-left) so it never displaces the native back button on the left,
        // which most sub-screens here still rely on instead of an in-content Back button.
        // Toggles rather than forcing true, so even if the state ever desyncs from what's on
        // screen the next tap always changes it instead of being swallowed.
        headerRight: () => <MenuButton onPress={() => setMenuOpen(open => !open)} />,
      }}
    >
      {/*
        New APP Videos/'s Access Point control panel selection.mp4 shows the Home dashboard with
        no teal header bar at all -- the hamburger, refresh, and panel-name pill sit inline on the
        gradient body itself (see HomeScreen.tsx's TopBar). Hiding the native header here only;
        every other screen keeps it.
      */}
      <Stack.Screen name="Home" component={HomeScreen} options={{ headerShown: false }} />
      <Stack.Screen name="ChangePassword" component={ChangePasswordScreen} options={{ title: t('Change password') }} />
      <Stack.Screen name="SelectPairingType" component={SelectPairingTypeScreen} options={{ title: t('Add a panel') }} />
      <Stack.Screen name="PairingIntro" component={PairingIntroScreen} options={{ title: t('Access Point') }} />
      <Stack.Screen name="ConnectToPanel" component={ConnectToPanelScreen} options={{ title: t('ACCESS POINT') }} />
      <Stack.Screen name="CreateDevice" component={CreateDeviceScreen} options={{ title: t('Add a panel') }} />
      <Stack.Screen name="SmsPairing" component={SmsPairingScreen} options={{ title: t('Add a panel') }} />
      <Stack.Screen
        name="PairingConfirmation"
        component={PairingConfirmationScreen}
        options={{ title: t('Add a panel'), headerBackVisible: false }}
      />
      <Stack.Screen name="PanelSelector" component={PanelSelectorScreen} options={{ title: t('Your panels') }} />
      <Stack.Screen name="ManagePanel" component={ManagePanelScreen} options={{ title: t('Manage panel') }} />
      <Stack.Screen name="PanelIdentification" component={PanelIdentificationScreen} options={{ title: t('Identify panel') }} />
      <Stack.Screen name="Zones" component={ZonesScreen} options={{ title: t('Zones') }} />
      <Stack.Screen name="Exclusions" component={ExclusionsScreen} options={{ title: t('Exclusions') }} />
      <Stack.Screen name="Memory" component={MemoryScreen} options={{ title: t('Memory') }} />
      <Stack.Screen name="Pgm" component={PgmScreen} options={{ title: t('SALIDAS PROGRAMABLES') }} />
      <Stack.Screen
        name="ScheduledDepartures"
        component={ScheduledDeparturesScreen}
        options={{ title: t('Scheduled departures') }}
      />
      <Stack.Screen name="PanelUsers" component={PanelUsersScreen} options={{ title: t('ETIQUETAS DE USUARIO') }} />
      <Stack.Screen name="Battery" component={BatteryScreen} options={{ title: t('Battery') }} />
      <Stack.Screen name="Failures" component={FailuresScreen} options={{ title: t('FALLAS') }} />
      <Stack.Screen name="Events" component={EventsScreen} options={{ title: t('EVENTOS') }} />
      <Stack.Screen name="CustomMessages" component={CustomMessagesScreen} options={{ title: t('Custom messages') }} />
      <Stack.Screen name="ContactPhone" component={ContactPhoneScreen} options={{ title: t('Contact numbers') }} />
      <Stack.Screen
        name="SmsPasswordChange"
        component={SmsPasswordChangeScreen}
        options={{ title: t('Change SMS PIN') }}
      />
      <Stack.Screen name="Clock" component={ClockScreen} options={{ title: t('PUESTA EN HORA') }} />
      <Stack.Screen name="InstallerMode" component={InstallerModeScreen} options={{ title: t('INSTALADOR') }} />
      <Stack.Screen name="Language" component={LanguageScreen} options={{ title: t('Language') }} />
      <Stack.Screen name="UserAccount" component={UserAccountScreen} options={{ title: t('User account') }} />
    </Stack.Navigator>
    <MainMenu visible={menuOpen} onClose={() => setMenuOpen(false)} />
    </>
  );
}

/**
 * @format
 */

import 'react-native-gesture-handler';
import { AppRegistry } from 'react-native';
import App from './App';
import { name as appName } from './app.json';
import { registerBackgroundHandler } from './src/notifications/messaging';

// Must run here, before the app component renders -- Firebase requires the background message
// handler to be registered outside the React tree, or messages received while the app is fully
// killed (not just backgrounded) never reach it.
registerBackgroundHandler();

AppRegistry.registerComponent(appName, () => App);

// Manual Jest mock -- same reason as react-native-tcp-socket.js next to this file. The real
// package builds an RNFBNativeEventEmitter at import time (@react-native-firebase/app's
// internal/RNFBNativeEventEmitter.ts), which throws outside a real native runtime, so merely
// importing src/notifications/messaging.ts was enough to fail the whole suite once the Firebase
// wiring landed. Nothing here exercises real FCM; this just lets the modules that import it load.
const noopUnsubscribe = () => {};

module.exports = {
  AuthorizationStatus: {
    NOT_DETERMINED: -1,
    DENIED: 0,
    AUTHORIZED: 1,
    PROVISIONAL: 2,
  },
  getMessaging: () => ({}),
  getToken: () => Promise.resolve('test-fcm-token'),
  requestPermission: () => Promise.resolve(1),
  onTokenRefresh: () => noopUnsubscribe,
  onMessage: () => noopUnsubscribe,
  onNotificationOpenedApp: () => noopUnsubscribe,
  getInitialNotification: () => Promise.resolve(null),
  setBackgroundMessageHandler: () => {},
};

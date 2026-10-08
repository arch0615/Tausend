// Manual Jest mock -- same reason as the other mocks in this folder. The real module touches its
// native interface at import time (@react-native-community/geolocation/js/nativeInterface.ts's
// GeolocationEventEmitter getter), which throws outside a real native runtime, so importing
// src/panels/emergencyLocation.ts was enough to fail the suite. Calls the error callback rather
// than inventing coordinates, which is the path emergencyLocation.ts already handles.
module.exports = {
  __esModule: true,
  default: {
    getCurrentPosition: (_success, error) => {
      if (error) error({ code: 2, message: 'position unavailable (test mock)' });
    },
    watchPosition: () => 0,
    clearWatch: () => {},
    stopObserving: () => {},
    setRNConfiguration: () => {},
    requestAuthorization: () => {},
  },
};

// Manual Jest mock -- same reason as the other mocks in this folder. notifee reaches for its
// native module at import time, which throws outside a device, so importing
// notifications/messaging.ts would fail the whole suite without this.
const AndroidImportance = { NONE: 0, MIN: 1, LOW: 2, DEFAULT: 3, HIGH: 4 };

const notifee = {
  displayNotification: jest.fn(async () => 'test-notification-id'),
  createChannel: jest.fn(async () => 'test-channel'),
  cancelNotification: jest.fn(async () => {}),
  onForegroundEvent: jest.fn(() => () => {}),
  onBackgroundEvent: jest.fn(),
  requestPermission: jest.fn(async () => ({ authorizationStatus: 1 })),
};

module.exports = notifee;
module.exports.default = notifee;
module.exports.AndroidImportance = AndroidImportance;

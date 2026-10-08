// Manual Jest mock -- same reason as the other mocks in this folder. The real package reaches
// for its native module at call time and throws "Native module is null" outside a device, so any
// test that mounts something touching storage (PanelContext's stored panel selection,
// LocaleContext's stored language) failed before reaching its own assertions.
//
// Backed by a real in-memory Map rather than bare jest.fn()s, so a test can write a value and
// read it back the way the app does. Call __resetAsyncStorage() in beforeEach if a test needs a
// clean slate.
const store = new Map();

const AsyncStorage = {
  getItem: jest.fn(async (key) => (store.has(key) ? store.get(key) : null)),
  setItem: jest.fn(async (key, value) => {
    store.set(key, String(value));
  }),
  removeItem: jest.fn(async (key) => {
    store.delete(key);
  }),
  clear: jest.fn(async () => {
    store.clear();
  }),
  getAllKeys: jest.fn(async () => [...store.keys()]),
  multiGet: jest.fn(async (keys) => keys.map((k) => [k, store.has(k) ? store.get(k) : null])),
  multiSet: jest.fn(async (pairs) => {
    pairs.forEach(([k, v]) => store.set(k, String(v)));
  }),
  multiRemove: jest.fn(async (keys) => {
    keys.forEach((k) => store.delete(k));
  }),
};

globalThis.__resetAsyncStorage = () => store.clear();

module.exports = AsyncStorage;
module.exports.default = AsyncStorage;

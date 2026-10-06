// Manual Jest mock -- the real module constructs a NativeEventEmitter at import time (see
// react-native-tcp-socket/src/Globals.js), which throws outside a real native runtime. Nothing in
// this test suite exercises actual socket I/O; this just lets modules that import the library
// load without crashing.
class MockSocket {
  on() {
    return this;
  }
  write(_data, _encoding, cb) {
    if (cb) cb();
    return true;
  }
  destroy() {
    return this;
  }
}

module.exports = {
  createConnection: () => new MockSocket(),
  connect: () => new MockSocket(),
};

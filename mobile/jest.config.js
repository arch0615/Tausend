module.exports = {
  preset: '@react-native/jest-preset',
  // The preset's own pattern only whitelists react-native itself for transformation --
  // @react-navigation, @react-native-* scoped packages (community, async-storage, ...), and the
  // react-native-* native-module packages all ship ESM in node_modules too, so they need to stay
  // transformable rather than ignored.
  transformIgnorePatterns: [
    'node_modules/(?!((jest-)?react-native|@react-native(-[a-z-]+)?|@react-navigation|react-native-.*)/)',
  ],
};

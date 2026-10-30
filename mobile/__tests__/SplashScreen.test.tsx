/**
 * Client, 30 Oct: "Logo 1 should be displayed as a splash screen when the app is opened.
 * Currently, the app icon is shown; please replace it with Logo 1."
 *
 * The splash renders before anything about the account is known, so the only Logo1 available to
 * it is whatever the last successful login on this device cached (branding/brandCache.ts). These
 * pin both halves of that: the bundled default when there is no cache, and the remembered custom
 * logo when there is -- against the real AsyncStorage mock rather than a stubbed cache module,
 * since the read-on-mount timing is the part worth covering.
 *
 * @format
 */

import React from 'react';
import ReactTestRenderer from 'react-test-renderer';
import { Image } from 'react-native';
import AsyncStorage from '@react-native-async-storage/async-storage';
import { SplashScreen } from '../src/screens/SplashScreen';
import { ThemeProvider } from '../src/theme/ThemeProvider';

async function mount() {
  let tree!: ReactTestRenderer.ReactTestRenderer;
  await ReactTestRenderer.act(async () => {
    tree = ReactTestRenderer.create(
      <ThemeProvider>
        <SplashScreen />
      </ThemeProvider>,
    );
  });
  return tree;
}

describe('splash screen Logo1', () => {
  beforeEach(() => {
    (globalThis as { __resetAsyncStorage?: () => void }).__resetAsyncStorage?.();
  });

  it('shows the bundled default logo1 when nothing is cached', async () => {
    const tree = await mount();
    const logo = tree.root.findByType(Image);
    // The exact asset, not merely "no uri" -- the client's complaint was that the wrong image
    // was showing, so pinning which one is the whole point.
    expect((logo.props.source as { testUri: string }).testUri).toMatch(/logo1-default\.png$/);
  });

  it('shows the remembered custom logo1 instead, once one is cached', async () => {
    await AsyncStorage.setItem('tausend-cached-brand-logo1', 'data:image/png;base64,resellerLogo');
    const tree = await mount();
    const logo = tree.root.findByType(Image);
    expect(logo.props.source).toEqual({ uri: 'data:image/png;base64,resellerLogo' });
  });

  it('renders no logo at all until the cache read resolves, rather than flashing the default', async () => {
    await AsyncStorage.setItem('tausend-cached-brand-logo1', 'data:image/png;base64,resellerLogo');
    let tree!: ReactTestRenderer.ReactTestRenderer;
    // Deliberately not awaiting the mount: this is the first synchronous frame, before the
    // AsyncStorage promise has settled.
    ReactTestRenderer.act(() => {
      tree = ReactTestRenderer.create(
        <ThemeProvider>
          <SplashScreen />
        </ThemeProvider>,
      );
    });
    expect(tree.root.findAllByType(Image)).toHaveLength(0);

    await ReactTestRenderer.act(async () => {});
    expect(tree.root.findByType(Image).props.source).toEqual({ uri: 'data:image/png;base64,resellerLogo' });
  });
});

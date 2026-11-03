/**
 * The opening screen, and the rule for when it is allowed to appear at all.
 *
 * Client, 30 Oct: "Logo 1 should be displayed as a splash screen when the app is opened."
 * Client, 2 Nov: "when opening the app for the first time, the splash screen must NOT appear" --
 * on a fresh install nothing is known about which reseller's customer this is, and the splash is
 * the one screen that is pure branding, so it is held back exactly once per install.
 *
 * Which logo it shows is decided by RootNavigator and passed in, so these cover the two halves
 * separately: the gate (claimFirstLaunch) and the rendering (SplashScreen).
 *
 * @format
 */

import React from 'react';
import ReactTestRenderer from 'react-test-renderer';
import { Image } from 'react-native';
import { SplashScreen } from '../src/screens/SplashScreen';
import { claimFirstLaunch, getCachedBrandLogo1, setCachedBrandLogo1 } from '../src/branding/brandCache';
import { ThemeProvider } from '../src/theme/ThemeProvider';

function mount(logo?: string) {
  let tree!: ReactTestRenderer.ReactTestRenderer;
  ReactTestRenderer.act(() => {
    tree = ReactTestRenderer.create(
      <ThemeProvider>
        <SplashScreen logo={logo} />
      </ThemeProvider>,
    );
  });
  return tree;
}

describe('splash gate (first launch)', () => {
  beforeEach(() => {
    (globalThis as { __resetAsyncStorage?: () => void }).__resetAsyncStorage?.();
  });

  it('claims the very first launch, so that launch shows no splash', async () => {
    await expect(claimFirstLaunch()).resolves.toBe(true);
  });

  it('does not claim any launch after the first, so those do show the splash', async () => {
    await claimFirstLaunch();
    await expect(claimFirstLaunch()).resolves.toBe(false);
    await expect(claimFirstLaunch()).resolves.toBe(false);
  });

  it('is independent of the brand cache, so a branded account still skips its first launch', async () => {
    await setCachedBrandLogo1('data:image/png;base64,resellerLogo');
    await expect(claimFirstLaunch()).resolves.toBe(true);
    // And the cached logo is untouched by the gate.
    await expect(getCachedBrandLogo1()).resolves.toBe('data:image/png;base64,resellerLogo');
  });
});

describe('splash rendering', () => {
  it("shows the client's bundled default logo1 when the account has no custom one", () => {
    const logo = mount().root.findByType(Image);
    expect((logo.props.source as { testUri: string }).testUri).toMatch(/logo1-default\.png$/);
  });

  it('shows the custom logo1 when one is passed in', () => {
    const logo = mount('data:image/png;base64,resellerLogo').root.findByType(Image);
    expect(logo.props.source).toEqual({ uri: 'data:image/png;base64,resellerLogo' });
  });

  it('fills the screen rather than sitting in a centred box', () => {
    // The client supplied a 1080x1920 asset and called it "full screen size", so anything less
    // than the whole screen would letterbox their art inside a box inside the screen.
    const logo = mount().root.findByType(Image);
    const style = Object.assign({}, ...[logo.props.style].flat().filter(Boolean));
    expect(style.width).toBe('100%');
    expect(style.height).toBe('100%');
    // contain, not cover: phones are taller than 9:16, and cover would crop the eagle's wings.
    expect(logo.props.resizeMode).toBe('contain');
  });
});

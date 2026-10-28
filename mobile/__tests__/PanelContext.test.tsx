/**
 * Covers the live-reachability signal added for client issues #2 and #16.
 *
 * The bug those report is one value read from the wrong place: the "Connected / No connection"
 * line and the panel selector's "Online / Offline" both read Devices.IsOnline, which is copied
 * out of the database when the session is created and never updated afterwards. It therefore
 * drifted both ways -- "No connection" for a panel that was working, and "Connected" after the
 * Wi-Fi was pulled. These tests pin the replacement: what a real command round-trip observed
 * wins, and the login-time flag is only a placeholder until the first observation lands.
 *
 * @format
 */

import React from 'react';
import ReactTestRenderer from 'react-test-renderer';
import { Text } from 'react-native';
import { PanelProvider, usePanels, resolveAccountBrand, type Panel, type Reachability } from '../src/panels/PanelContext';

let mockEmail = 'a@example.com';
jest.mock('../src/auth/AuthContext', () => ({
  useAuth: () => ({
    session: {
      account: {
        Email: mockEmail,
        Devices: [{ DeviceId: 7, Description: 'Bench', Mac: 'CRS186-0700198002', Pin: '2020', IsOnline: true }],
        SmsDevices: [],
      },
    },
  }),
}));

type Api = ReturnType<typeof usePanels>;

async function harness() {
  let api: Api | undefined;
  function Probe() {
    api = usePanels();
    return <Text>probe</Text>;
  }
  // The provider hydrates its stored selection from AsyncStorage on mount, so the initial
  // render has to be inside act() too, not just the later reportReachability calls.
  await ReactTestRenderer.act(async () => {
    ReactTestRenderer.create(
      <PanelProvider>
        <Probe />
      </PanelProvider>,
    );
  });
  return { get: () => api as Api };
}

const read = (api: Api): Reachability => api.reachabilityOf('ip', 7);

describe('live panel reachability', () => {
  beforeEach(() => {
    mockEmail = 'a@example.com';
  });

  it('starts out unknown, so the login-time flag is still the fallback', async () => {
    const h = await harness();
    expect(read(h.get())).toBe('unknown');
  });

  it('reports offline once a command round-trip fails, even though IsOnline said true', async () => {
    const h = await harness();
    await ReactTestRenderer.act(async () => {
      h.get().reportReachability('ip', 7, false);
    });
    // This is issue #16: IsOnline is true on the account payload above, and the screen used to
    // keep saying "Connected" because of it.
    expect(read(h.get())).toBe('offline');
  });

  it('recovers to online when the panel answers again', async () => {
    const h = await harness();
    await ReactTestRenderer.act(async () => {
      h.get().reportReachability('ip', 7, false);
    });
    await ReactTestRenderer.act(async () => {
      h.get().reportReachability('ip', 7, true);
    });
    // Issue #2 is the same fault in the other direction: a working panel shown as disconnected.
    expect(read(h.get())).toBe('online');
  });

  it('keeps each panel separate', async () => {
    const h = await harness();
    await ReactTestRenderer.act(async () => {
      h.get().reportReachability('ip', 7, false);
      h.get().reportReachability('ip', 99, true);
    });
    expect(h.get().reachabilityOf('ip', 7)).toBe('offline');
    expect(h.get().reachabilityOf('ip', 99)).toBe('online');
  });

  it('does not confuse an SMS panel with an IP panel of the same id', async () => {
    const h = await harness();
    await ReactTestRenderer.act(async () => {
      h.get().reportReachability('ip', 7, true);
    });
    expect(h.get().reachabilityOf('sms', 7)).toBe('unknown');
  });
});

/**
 * White-label branding (client, 26/27/28 Oct): the account's "effective brand" for screens that
 * show a logo regardless of which panel is selected -- both Logo1 (login screen, portrait) and
 * Logo2 (home screen banner, landscape) come from the same panel, never mixed from two different
 * ones, and each is returned as a ready-to-render data URI with the real stored mime type baked
 * in (not a hardcoded image/jpeg), since a PNG shown as if it were a JPEG loses its transparency.
 * A pure function, so tested directly against hand-built panel lists rather than through the
 * provider harness above.
 */
describe('resolveAccountBrand', () => {
  const ip = (deviceId: number, logo1Base64?: string, logo2Base64?: string, mimeType = 'image/jpeg'): Panel => ({
    kind: 'ip',
    deviceId,
    description: `Panel ${deviceId}`,
    identifier: `ID${deviceId}`,
    logo1Base64,
    logo1MimeType: logo1Base64 ? mimeType : undefined,
    logo2Base64,
    logo2MimeType: logo2Base64 ? mimeType : undefined,
  });
  const sms = (deviceId: number): Panel => ({ kind: 'sms', deviceId, description: `Sms ${deviceId}`, identifier: `S${deviceId}` });

  it('returns undefined when no panel has a custom logo', () => {
    expect(resolveAccountBrand([ip(1), ip(2), sms(3)])).toBeUndefined();
  });

  it('returns both logos from the first IP panel that has either set, as data URIs', () => {
    expect(resolveAccountBrand([ip(1), ip(2, 'logo1B', 'logo2B'), ip(3, 'logo1C', 'logo2C')])).toEqual({
      logo1Uri: 'data:image/jpeg;base64,logo1B',
      logo2Uri: 'data:image/jpeg;base64,logo2B',
    });
  });

  it('matches on either logo being set, not requiring both', () => {
    expect(resolveAccountBrand([ip(1), ip(2, 'logo1B', undefined)])).toEqual({
      logo1Uri: 'data:image/jpeg;base64,logo1B',
      logo2Uri: undefined,
    });
  });

  it('never picks up a logo from an SMS panel, even first in the list', () => {
    // SMS panels have no logo fields at all -- this just pins that the 'ip' filter actually runs
    // rather than happening to pass because none of the test's SMS panels could match anyway.
    expect(resolveAccountBrand([sms(1), ip(2, 'logo1B', 'logo2B')])).toEqual({
      logo1Uri: 'data:image/jpeg;base64,logo1B',
      logo2Uri: 'data:image/jpeg;base64,logo2B',
    });
  });

  it('bakes the real stored mime type into the URI, not a hardcoded image/jpeg', () => {
    // Client, 28 Oct: "it should admit both with and without background so it should be JPG or
    // PNG" -- a PNG rendered under a forced image/jpeg data URI loses its transparency.
    expect(resolveAccountBrand([ip(1, 'logo1B', 'logo2B', 'image/png')])).toEqual({
      logo1Uri: 'data:image/png;base64,logo1B',
      logo2Uri: 'data:image/png;base64,logo2B',
    });
  });

  it('returns undefined for an empty panel list', () => {
    expect(resolveAccountBrand([])).toBeUndefined();
  });
});

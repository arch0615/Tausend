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
import { PanelProvider, usePanels, type Reachability } from '../src/panels/PanelContext';

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

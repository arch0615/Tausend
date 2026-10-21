/**
 * Covers the client's 22 Oct request for "Selector de equipo":
 *
 * a) The screen must check each device's live connection status itself and show it directly
 *    (a red or green circle), rather than requiring a visit into each panel first. reachabilityOf
 *    only gets filled in by a screen that actually sends a command, so a panel nobody has opened
 *    yet fell back to the stale login-time flag here -- this screen now probes every IP panel on
 *    the list itself, in parallel, as soon as it is shown.
 * b) The list must be sorted alphabetically by name.
 *
 * These tests pin: the alphabetical order across both panel kinds, that the probe is sent for
 * every IP panel and never for an SMS one, that the dot lands on the right colour once each
 * probe resolves, and that the old "Online"/"Offline" text is gone.
 *
 * @format
 */

import React from 'react';
import ReactTestRenderer, { act } from 'react-test-renderer';
import { Text } from 'react-native';
import { PanelSelectorScreen } from '../src/screens/panels/PanelSelectorScreen';
import { StatusDot } from '../src/components/StatusDot';
import { ThemeProvider } from '../src/theme/ThemeProvider';
import { LocaleProvider } from '../src/i18n/LocaleContext';
import { palette } from '../src/theme/colors';

jest.mock('@react-navigation/native', () => {
  const actual = jest.requireActual('@react-navigation/native');
  const React = require('react');
  return {
    ...actual,
    useFocusEffect: (cb: React.EffectCallback) => React.useEffect(cb, [cb]),
  };
});

const mockGetGeneralStatus = jest.fn();
jest.mock('../src/api/command', () => ({
  getGeneralStatus: (deviceId: number) => mockGetGeneralStatus(deviceId),
}));

// A real in-memory reachability store, same shape as PanelContext's own, so reportReachability
// genuinely changes what reachabilityOf returns and the screen re-renders with it -- not just a
// static canned value standing in for "already resolved".
let mockReachability: Record<string, boolean> = {};
let mockRerenderListener: (() => void) | null = null;
jest.mock('../src/panels/PanelContext', () => {
  const React = require('react');
  // Module-level, not re-created per render: PanelContext's own `panels` is useMemo'd on
  // [session] alone, so a stand-in that hands back a fresh array/functions every call would
  // make the screen's own useCallback([panels, reportReachability]) deps churn every render --
  // the effect never settles and re-fires forever instead of running once per focus.
  // isOnline deliberately set opposite to what the probe resolves in the colouring test below, so
  // a passing assertion there can only mean the live probe actually overrode the stale flag -- not
  // that the dot happened to already agree with it before any probe ran.
  const PANELS = [
    { kind: 'ip', deviceId: 1, description: 'Zebra Panel', identifier: 'Z1', isOnline: true },
    { kind: 'sms', deviceId: 3, description: 'Middle Panel', identifier: 'M1' },
    { kind: 'ip', deviceId: 2, description: 'Alpha Panel', identifier: 'A1', isOnline: false },
  ];
  return {
  usePanels: () => {
    const [, setTick] = React.useState(0);
    mockRerenderListener = () => setTick((n: number) => n + 1);
    const reachabilityOf = React.useCallback((kind: string, deviceId: number) => {
      const v = mockReachability[`${kind}:${deviceId}`];
      return v === undefined ? 'unknown' : v ? 'online' : 'offline';
    }, []);
    const reportReachability = React.useCallback((kind: string, deviceId: number, online: boolean) => {
      mockReachability[`${kind}:${deviceId}`] = online;
      mockRerenderListener?.();
    }, []);
    return {
      panels: PANELS,
      selected: null,
      selectPanel: jest.fn(),
      reachabilityOf,
      reportReachability,
    };
  },
  };
});

// Tracked so afterEach can unmount it: the screen's probe effect only cancels itself on
// unmount, and without that an earlier test's still-retrying probe can resolve mid the next
// test and call that test's reportReachability instead of its own.
let activeTree: ReactTestRenderer.ReactTestRenderer | null = null;

function render() {
  let tree!: ReactTestRenderer.ReactTestRenderer;
  act(() => {
    tree = ReactTestRenderer.create(
      <ThemeProvider>
        <LocaleProvider>
          <PanelSelectorScreen navigation={{ navigate: jest.fn() } as never} />
        </LocaleProvider>
      </ThemeProvider>,
    );
  });
  activeTree = tree;
  return tree;
}

const labels = (tree: ReactTestRenderer.ReactTestRenderer) =>
  tree.root.findAllByType(Text).flatMap((n) => (typeof n.props.children === 'string' ? [n.props.children] : []));

beforeEach(() => {
  mockReachability = {};
  mockGetGeneralStatus.mockReset();
  mockGetGeneralStatus.mockResolvedValue({ State: 0, Text: 'READY' });
});

afterEach(() => {
  act(() => {
    activeTree?.unmount();
  });
  activeTree = null;
});

describe('Selector de equipo, live status', () => {
  it('lists the panels alphabetically by name, mixing IP and SMS', async () => {
    const tree = render();
    await act(async () => {});
    const labelled = labels(tree);
    const order = ['Alpha Panel', 'Middle Panel', 'Zebra Panel'].map((n) => labelled.indexOf(n));
    expect(order.every((i) => i >= 0)).toBe(true);
    expect(order).toEqual([...order].sort((a, b) => a - b));
  });

  it('probes every IP panel and never an SMS one', async () => {
    render();
    await act(async () => {});
    expect(mockGetGeneralStatus).toHaveBeenCalledWith(1);
    expect(mockGetGeneralStatus).toHaveBeenCalledWith(2);
    expect(mockGetGeneralStatus).not.toHaveBeenCalledWith(3);
  });

  it('colours each dot once its own probe resolves, green when it answers and red when it does not', async () => {
    mockGetGeneralStatus.mockImplementation((deviceId: number) =>
      deviceId === 2 ? Promise.resolve({ State: 0, Text: 'READY' }) : Promise.resolve({ State: 3, Text: null }),
    );
    const tree = render();
    // Zebra's probe never answers READY, so it burns through both PROBE_ATTEMPTS with a real
    // PROBE_RETRY_DELAY_MS wait in between -- long enough to outlast a couple of microtask ticks,
    // so this has to actually wait out that retry rather than just flushing the promise queue.
    await act(async () => {
      await new Promise((resolve) => setTimeout(resolve, 600));
    });

    const dots = tree.root.findAllByType(StatusDot);
    expect(dots).toHaveLength(2); // one per IP panel, none for the SMS panel

    const colourByLabel = (name: string) => {
      const textNode = tree.root.findAllByType(Text).find((n) => n.props.children === name)!;
      // walk up to the row, then find the one StatusDot inside it
      let row = textNode;
      while (!row.findAllByType(StatusDot).length) row = row.parent!;
      return row.findAllByType(StatusDot)[0].props.color;
    };
    expect(colourByLabel('Alpha Panel')).toBe(palette.okInk);
    expect(colourByLabel('Zebra Panel')).toBe(palette.danger);
  });

  it('no longer shows the Online/Offline text the dot replaced', async () => {
    const tree = render();
    await act(async () => {});
    const labelled = labels(tree);
    expect(labelled).not.toContain('Online');
    expect(labelled).not.toContain('Offline');
  });
});

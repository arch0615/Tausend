/**
 * Covers the client's 20 Oct mockup: "Modify the SMS screen and add icons." The SMS dashboard
 * kept its pre-redesign look (five stacked text buttons, no icons) while the IP dashboard and
 * the Panico/Asalto/Emergencia row below it both got the previous-app treatment earlier -- this
 * was the one piece left over.
 *
 * The mockup is specific about three things these tests pin: the three arm modes become icon
 * tiles in away/night/day order (matching the IP dashboard's own order), Desarmar and Consultar
 * estado move below the tiles in that order, and the tile and Consultar estado colours are the
 * header's dark teal, not the IP dashboard's green -- sampled directly from the mockup image.
 *
 * @format
 */

import React from 'react';
import ReactTestRenderer, { act } from 'react-test-renderer';
import { Text } from 'react-native';
import { HomeScreen } from '../src/screens/main/HomeScreen';
import { ThemeProvider } from '../src/theme/ThemeProvider';
import { LocaleProvider } from '../src/i18n/LocaleContext';
import { palette } from '../src/theme/colors';

// HomeScreen always renders MainMenu as a sibling (hidden until opened), and MainMenu calls
// useNavigation() unconditionally -- it is not under test here, so stand in a plain object
// rather than require a real NavigationContainer + registered screen.
jest.mock('@react-navigation/native', () => ({
  ...jest.requireActual('@react-navigation/native'),
  useNavigation: () => ({ navigate: jest.fn() }),
}));

const mockOpenSmsComposer = jest.fn().mockResolvedValue(true);
jest.mock('../src/pairing/smsIntent', () => ({
  openSmsComposer: (phone: string, body: string) => mockOpenSmsComposer(phone, body),
}));
jest.mock('../src/panels/PanelContext', () => ({
  usePanels: () => ({ panels: [{ kind: 'sms', deviceId: 21, description: 'Sms1' }], selected: { kind: 'sms', deviceId: 21, description: 'Sms1' } }),
}));
jest.mock('../src/auth/AuthContext', () => ({
  useAuth: () => ({
    session: { account: { Devices: [], SmsDevices: [{ DeviceId: 21, Description: 'Sms1', Identifier: 'cr832', DevicePin: '1234', SimPin: '4321', PhoneNumber: '+5491155512345', DeviceType: 'cr832' }] } },
  }),
}));

function render() {
  let tree!: ReactTestRenderer.ReactTestRenderer;
  act(() => {
    tree = ReactTestRenderer.create(
      <ThemeProvider>
        <LocaleProvider>
          <HomeScreen
            route={{ key: 'k', name: 'Home' } as never}
            navigation={{ navigate: jest.fn(), setOptions: jest.fn(), goBack: jest.fn() } as never}
          />
        </LocaleProvider>
      </ThemeProvider>,
    );
  });
  return tree;
}

const labelOrder = (tree: ReactTestRenderer.ReactTestRenderer) =>
  tree.root.findAllByType(Text).flatMap((n) => (typeof n.props.children === 'string' ? [n.props.children] : []));

// Walk up to the nearest ancestor that actually handles the tap, rather than matching react-
// native's Pressable by reference -- in this test environment that type is not the same object
// the app code renders with, even though the tree structurally contains a node named Pressable.
function findPressableAncestor(node: ReactTestRenderer.ReactTestInstance) {
  let n = node;
  while (typeof n.props.onPress !== 'function') n = n.parent!;
  return n;
}

// onPress here is send(), an async function -- awaiting it inside act() matters: letting it
// float left the next test's renderer in a broken state ("Can't access .root on unmounted test
// renderer"), not just an act() warning.
async function pressByLabel(tree: ReactTestRenderer.ReactTestRenderer, label: string) {
  const textNode = tree.root.findAllByType(Text).find((n) => n.props.children === label);
  if (!textNode) throw new Error(`no text node for "${label}"`);
  await act(async () => {
    await findPressableAncestor(textNode).props.onPress();
  });
}

describe('SMS dashboard, arm/disarm/status section', () => {
  beforeEach(() => mockOpenSmsComposer.mockClear());

  it('shows the three arm modes as icon tiles, then Desarmar, then Consultar estado, in that order', () => {
    const labels = labelOrder(render());
    const order = ['Armar modo ausente', 'Armar modo noche', 'Armar modo día', 'Desarmar', 'Consultar estado'].map((l) =>
      labels.indexOf(l),
    );
    expect(order.every((i) => i >= 0)).toBe(true);
    expect(order).toEqual([...order].sort((a, b) => a - b));
  });

  it('wires each tile and button to its own command, not a neighbour\'s', async () => {
    const tree = render();
    await pressByLabel(tree, 'Armar modo ausente');
    expect(mockOpenSmsComposer).toHaveBeenLastCalledWith('+5491155512345', 'alarmas*4321ARA*tausend');

    await pressByLabel(tree, 'Armar modo noche');
    expect(mockOpenSmsComposer).toHaveBeenLastCalledWith('+5491155512345', 'alarmas*4321ARN*tausend');

    await pressByLabel(tree, 'Armar modo día');
    expect(mockOpenSmsComposer).toHaveBeenLastCalledWith('+5491155512345', 'alarmas*4321ARD*tausend');

    await pressByLabel(tree, 'Desarmar');
    expect(mockOpenSmsComposer).toHaveBeenLastCalledWith('+5491155512345', 'alarmas*4321DAR*tausend');

    await pressByLabel(tree, 'Consultar estado');
    expect(mockOpenSmsComposer).toHaveBeenLastCalledWith('+5491155512345', 'alarmas*4321STS*tausend');
  });

  it('colours the arm tiles and Consultar estado with the header teal, not the IP dashboard green', () => {
    const tree = render();
    const flatten = (s: unknown): Record<string, unknown> =>
      Object.assign({}, ...(Array.isArray(s) ? s.filter(Boolean).map(flatten) : [s]));

    for (const label of ['Armar modo ausente', 'Armar modo noche', 'Armar modo día']) {
      const textNode = tree.root.findAllByType(Text).find((n) => n.props.children === label)!;
      const node = findPressableAncestor(textNode);
      expect(flatten(node.props.style).backgroundColor).toBe(palette.headerBg);
      expect(flatten(node.props.style).backgroundColor).not.toBe(palette.accent);
    }
  });
});

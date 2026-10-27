/**
 * Covers the client's 17 Oct request for the SMS panel's "Gestionar central" screen:
 * "Identificador" should read "Numero de telefono" and "Codigo de usuario" should read
 * "Clave SMS".
 *
 * They assumed it was only a label change. It was not: SmsPairingScreen stores the panel model
 * in Identifier (cr832 and similar) and the real number in PhoneNumber, so renaming the label
 * alone would have shown the model under a heading saying "phone number". These tests pin both
 * halves -- the label text, and that the box next to it holds the number and not the model --
 * and that an IP panel is untouched.
 *
 * @format
 */

import React from 'react';
import ReactTestRenderer from 'react-test-renderer';
import { Alert, Text } from 'react-native';
import { ManagePanelScreen } from '../src/screens/panels/ManagePanelScreen';
import { ThemeProvider } from '../src/theme/ThemeProvider';
import { LocaleProvider } from '../src/i18n/LocaleContext';



// jest.mock factories are hoisted above every const in this file, so the fixtures live inside
// them. The names are prefixed with "mock" because that is the only out-of-scope reference jest
// allows a factory to close over.
jest.mock('../src/panels/PanelContext', () => ({
  usePanels: () => ({
    panels: [
      { kind: 'ip', deviceId: 7, description: 'Prueba', identifier: 'CPF6951000298001' },
      { kind: 'sms', deviceId: 11, description: 'Sms2', identifier: 'cr832' },
    ],
  }),
}));
const mockGetCurrentDeviceToken = jest.fn();
jest.mock('../src/notifications/messaging', () => ({
  getCurrentDeviceToken: () => mockGetCurrentDeviceToken(),
}));
const mockGetNotificationMute = jest.fn();
const mockSetNotificationMute = jest.fn();
jest.mock('../src/api/notifications', () => ({
  getNotificationMute: (identifier: string, token: string) => mockGetNotificationMute(identifier, token),
  setNotificationMute: (identifier: string, token: string, muted: boolean) =>
    mockSetNotificationMute(identifier, token, muted),
}));
jest.mock('../src/auth/AuthContext', () => ({
  useAuth: () => ({
    logout: jest.fn(),
    refreshSession: jest.fn(),
    session: {
      account: {
        Devices: [{ DeviceId: 7, Description: 'Prueba', Mac: 'CPF6951000298001', IsOnline: true, Pin: '2222' }],
        SmsDevices: [
          {
            DeviceId: 11,
            Description: 'Sms2',
            Identifier: 'cr832',            // the model, which is what used to be shown
            DevicePin: '1234',
            SimPin: '1234',
            PhoneNumber: '+5491155512345',  // what the client expects to see
            DeviceType: 'cr832',
          },
        ],
      },
    },
  }),
}));

beforeEach(() => {
  mockGetCurrentDeviceToken.mockReset().mockResolvedValue('mock-device-token');
  mockGetNotificationMute.mockReset().mockResolvedValue({ State: 0, Code: 0, Message: 'OK', Muted: false });
  mockSetNotificationMute.mockReset().mockResolvedValue({ State: 0, Code: 0, Message: 'OK' });
});

// Async: ManagePanelScreen fetches this device's current mute state on mount (see "Apagar
// notificaciones", 27 Oct) -- awaiting flushes that chain so it never dangles past the test that
// triggered it, the same issue PanelSelectorScreen.test.tsx hit with its own probe-on-focus effect.
async function render(kind: 'ip' | 'sms', deviceId: number) {
  let tree!: ReactTestRenderer.ReactTestRenderer;
  await ReactTestRenderer.act(async () => {
    tree = ReactTestRenderer.create(
      <ThemeProvider>
        <LocaleProvider>
          <ManagePanelScreen
            route={{ key: 'k', name: 'ManagePanel', params: { kind, deviceId } } as never}
            navigation={{ navigate: jest.fn(), goBack: jest.fn(), setOptions: jest.fn() } as never}
          />
        </LocaleProvider>
      </ThemeProvider>,
    );
  });
  return tree;
}

const labels = (tree: ReactTestRenderer.ReactTestRenderer) =>
  tree.root.findAllByType(Text).flatMap((n) => (typeof n.props.children === 'string' ? [n.props.children] : []));

const fieldValues = (tree: ReactTestRenderer.ReactTestRenderer) =>
  tree.root.findAllByType(require('react-native').TextInput).map((n) => n.props.value);

describe('Gestionar central, SMS panel', () => {
  it('labels the fields the way the client asked', async () => {
    const l = labels(await render('sms', 11));
    expect(l).toContain('Número de teléfono');
    expect(l).toContain('Clave SMS');
    expect(l).not.toContain('Identificador');
    expect(l).not.toContain('Código de usuario');
  });

  it('shows the phone number in that box, not the panel model', async () => {
    const values = fieldValues(await render('sms', 11));
    expect(values).toContain('+5491155512345');
    expect(values).not.toContain('cr832');
  });
});

describe('Gestionar central, IP panel', () => {
  it('keeps its own labels and its identifier', async () => {
    const tree = await render('ip', 7);
    const l = labels(tree);
    expect(l).toContain('Identificador');
    expect(l).toContain('Código de usuario');
    expect(l).not.toContain('Número de teléfono');
    expect(fieldValues(tree)).toContain('CPF6951000298001');
  });
});

/**
 * Client mockup, 20 Oct: "BLOQUEAR CENTRAL: Change to DESVINCULAR PARA TODOS" /
 * "REESTABLECER CENTRAL: Change to ELIMINAR PARA TODOS".
 *
 * Their own mockup for the first is a screenshot of the real dialog with only the title and
 * button relabelled -- body text untouched, so that one is pinned as a pure rename. Their mockup
 * for the second is a different, hand-made graphic with its own short new text, backed by a
 * written spec calling for the panel's identifier to be deleted from the server, so that one is
 * pinned on title, button AND body text, since all three changed.
 */
describe('Gestionar central, IP panel, the two account-wide actions', () => {
  function pressButton(tree: ReactTestRenderer.ReactTestRenderer, label: string) {
    const textNode = tree.root.findAllByType(Text).find((n) => n.props.children === label)!;
    let node = textNode;
    while (typeof node.props.onPress !== 'function') node = node.parent!;
    ReactTestRenderer.act(() => node.props.onPress());
  }

  let alertSpy: jest.SpyInstance;
  beforeEach(() => {
    alertSpy = jest.spyOn(Alert, 'alert').mockImplementation(() => {});
  });
  afterEach(() => alertSpy.mockRestore());

  it('shows the new button text and not the old', async () => {
    const l = labels(await render('ip', 7));
    expect(l).toContain('Desvincular para todos');
    expect(l).toContain('Eliminar para todos');
    expect(l).not.toContain('Bloquear esta central');
    expect(l).not.toContain('Restablecer esta central');
    // the section header above them is not part of this request and stays as it was
    expect(l).toContain('Bloqueo por código de usuario');
  });

  it('keeps the unlink dialog\'s own body text unchanged -- a title and button rename only', async () => {
    pressButton(await render('ip', 7), 'Desvincular para todos');
    expect(alertSpy).toHaveBeenCalledWith(
      '¿Desvincular para todos?',
      'Esto revoca de inmediato el control de la app sobre la central y cierra la sesión de todos los que la tienen vinculada, incluido tú. Para restaurar el acceso, vinculala de nuevo con el mismo identificador y código de usuario.',
      expect.arrayContaining([expect.objectContaining({ text: 'Desvincular' })]),
    );
  });

  it('gives the delete dialog its own new title, body and button text', async () => {
    pressButton(await render('ip', 7), 'Eliminar para todos');
    expect(alertSpy).toHaveBeenCalledWith(
      '¿Eliminar para todos?',
      'Esto elimina la central de todas las cuentas.',
      expect.arrayContaining([expect.objectContaining({ text: 'Eliminar' })]),
    );
  });
});

/**
 * Client, 27 Oct: "Apagar notificaciones" on Gestionar central -- mutes push for this one panel
 * on this one phone only. These tests pin: the button reflects the real state fetched from the
 * server on open (not just a locally-guessed default), muting asks for confirmation first and
 * sends this phone's own token, and un-muting does not ask and just sends the opposite call.
 */
describe('Gestionar central, IP panel, notification mute', () => {
  function pressButton(tree: ReactTestRenderer.ReactTestRenderer, label: string) {
    const textNode = tree.root.findAllByType(Text).find((n) => n.props.children === label)!;
    let node = textNode;
    while (typeof node.props.onPress !== 'function') node = node.parent!;
    ReactTestRenderer.act(() => node.props.onPress());
  }

  let alertSpy: jest.SpyInstance;
  beforeEach(() => {
    alertSpy = jest.spyOn(Alert, 'alert').mockImplementation(() => {});
  });
  afterEach(() => alertSpy.mockRestore());

  it('shows "turn off" when the server reports this phone as not muted', async () => {
    mockGetNotificationMute.mockResolvedValue({ State: 0, Code: 0, Message: 'OK', Muted: false });
    const l = labels(await render('ip', 7));
    expect(l).toContain('Apagar notificaciones');
    expect(l).not.toContain('Encender notificaciones');
    expect(mockGetNotificationMute).toHaveBeenCalledWith('CPF6951000298001', 'mock-device-token');
  });

  it('shows "turn on" when the server reports this phone as already muted', async () => {
    mockGetNotificationMute.mockResolvedValue({ State: 0, Code: 0, Message: 'OK', Muted: true });
    const l = labels(await render('ip', 7));
    expect(l).toContain('Encender notificaciones');
    expect(l).not.toContain('Apagar notificaciones');
  });

  it('confirms before muting, then sends this phone\'s own identifier and token', async () => {
    mockGetNotificationMute.mockResolvedValue({ State: 0, Code: 0, Message: 'OK', Muted: false });
    const tree = await render('ip', 7);
    pressButton(tree, 'Apagar notificaciones');
    expect(alertSpy).toHaveBeenCalledWith(
      'Atención',
      'Ésta acción solo apaga las notificaciones en éste dispositivo',
      expect.arrayContaining([expect.objectContaining({ text: 'Aceptar' })]),
    );
    expect(mockSetNotificationMute).not.toHaveBeenCalled();

    // Simulates the user tapping the dialog's own OK button, not the screen's button again.
    const onOk = alertSpy.mock.calls[0][2][0].onPress;
    await ReactTestRenderer.act(async () => onOk());
    expect(mockSetNotificationMute).toHaveBeenCalledWith('CPF6951000298001', 'mock-device-token', true);
  });

  it('un-mutes directly, with no confirmation dialog', async () => {
    mockGetNotificationMute.mockResolvedValue({ State: 0, Code: 0, Message: 'OK', Muted: true });
    const tree = await render('ip', 7);
    // Not pressButton here: unlike every other button on this screen, this one's own onPress
    // calls an async function (applyMute) directly rather than a sync Alert.alert -- pressButton's
    // sync act() wrapper can't track that work, so this awaits it directly instead.
    const textNode = tree.root.findAllByType(Text).find((n) => n.props.children === 'Encender notificaciones')!;
    let node = textNode;
    while (typeof node.props.onPress !== 'function') node = node.parent!;
    await ReactTestRenderer.act(async () => node.props.onPress());
    expect(alertSpy).not.toHaveBeenCalled();
    expect(mockSetNotificationMute).toHaveBeenCalledWith('CPF6951000298001', 'mock-device-token', false);
  });
});

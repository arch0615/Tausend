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

function render(kind: 'ip' | 'sms', deviceId: number) {
  let tree!: ReactTestRenderer.ReactTestRenderer;
  ReactTestRenderer.act(() => {
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
  it('labels the fields the way the client asked', () => {
    const l = labels(render('sms', 11));
    expect(l).toContain('Número de teléfono');
    expect(l).toContain('Clave SMS');
    expect(l).not.toContain('Identificador');
    expect(l).not.toContain('Código de usuario');
  });

  it('shows the phone number in that box, not the panel model', () => {
    const values = fieldValues(render('sms', 11));
    expect(values).toContain('+5491155512345');
    expect(values).not.toContain('cr832');
  });
});

describe('Gestionar central, IP panel', () => {
  it('keeps its own labels and its identifier', () => {
    const tree = render('ip', 7);
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

  it('shows the new button text and not the old', () => {
    const l = labels(render('ip', 7));
    expect(l).toContain('Desvincular para todos');
    expect(l).toContain('Eliminar para todos');
    expect(l).not.toContain('Bloquear esta central');
    expect(l).not.toContain('Restablecer esta central');
    // the section header above them is not part of this request and stays as it was
    expect(l).toContain('Bloqueo por código de usuario');
  });

  it('keeps the unlink dialog\'s own body text unchanged -- a title and button rename only', () => {
    pressButton(render('ip', 7), 'Desvincular para todos');
    expect(alertSpy).toHaveBeenCalledWith(
      '¿Desvincular para todos?',
      'Esto revoca de inmediato el control de la app sobre la central y cierra la sesión de todos los que la tienen vinculada, incluido tú. Para restaurar el acceso, vinculala de nuevo con el mismo identificador y código de usuario.',
      expect.arrayContaining([expect.objectContaining({ text: 'Desvincular' })]),
    );
  });

  it('gives the delete dialog its own new title, body and button text', () => {
    pressButton(render('ip', 7), 'Eliminar para todos');
    expect(alertSpy).toHaveBeenCalledWith(
      '¿Eliminar para todos?',
      'Esto elimina la central de todas las cuentas.',
      expect.arrayContaining([expect.objectContaining({ text: 'Eliminar' })]),
    );
  });
});

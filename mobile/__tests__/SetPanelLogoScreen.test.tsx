/**
 * Covers the three things the client asked for on 30 Oct about the custom logo screen:
 * "you can modify one or both images; it is not mandatory to change both", "it must also be
 * possible to restore the default images using a reset to default image button", and the fault
 * behind "the logos do not change" -- an upload used to stay invisible until every linked account
 * logged in again, because the logos travel inside the account payload.
 *
 * @format
 */

import React from 'react';
import ReactTestRenderer from 'react-test-renderer';
import { Text, TextInput } from 'react-native';
import { SetPanelLogoScreen } from '../src/screens/panels/SetPanelLogoScreen';
import { ThemeProvider } from '../src/theme/ThemeProvider';
import { LocaleProvider } from '../src/i18n/LocaleContext';

const mockSetDeviceLogos = jest.fn();
const mockClearDeviceLogos = jest.fn();
jest.mock('../src/api/device', () => ({
  setDeviceLogos: (...args: unknown[]) => mockSetDeviceLogos(...args),
  clearDeviceLogos: (...args: unknown[]) => mockClearDeviceLogos(...args),
}));

const mockRefreshSession = jest.fn().mockResolvedValue(undefined);
jest.mock('../src/auth/AuthContext', () => ({
  useAuth: () => ({ refreshSession: mockRefreshSession }),
}));

jest.mock('@react-navigation/native', () => ({
  ...jest.requireActual('@react-navigation/native'),
  useNavigation: () => ({ goBack: jest.fn(), navigate: jest.fn() }),
}));

// The picker is the one piece a test cannot drive for real; each test decides what it hands back.
const mockLaunchImageLibrary = jest.fn();
jest.mock('react-native-image-picker', () => ({
  launchImageLibrary: () => mockLaunchImageLibrary(),
}));

const OK = { State: 0 };

async function mount() {
  let tree!: ReactTestRenderer.ReactTestRenderer;
  await ReactTestRenderer.act(async () => {
    tree = ReactTestRenderer.create(
      <ThemeProvider>
        <LocaleProvider>
          <SetPanelLogoScreen />
        </LocaleProvider>
      </ThemeProvider>,
    );
  });
  return tree;
}

function inputs(tree: ReactTestRenderer.ReactTestRenderer) {
  return tree.root.findAllByType(TextInput);
}

async function type(tree: ReactTestRenderer.ReactTestRenderer, which: number, text: string) {
  await ReactTestRenderer.act(async () => {
    inputs(tree)[which].props.onChangeText(text);
  });
}

// onPress is async on every button here, so it is awaited rather than fired and forgotten --
// letting it float leaves the next test's renderer in a broken state, not just an act() warning.
async function press(tree: ReactTestRenderer.ReactTestRenderer, title: string) {
  const button = tree.root.findAll((n) => typeof n.props?.onPress === 'function' && n.props?.title === title)[0];
  if (!button) throw new Error(`no button titled "${title}"`);
  await ReactTestRenderer.act(async () => {
    await button.props.onPress();
  });
}

const screenText = (tree: ReactTestRenderer.ReactTestRenderer) =>
  tree.root.findAllByType(Text).map((n) => n.props.children).join(' | ');

async function fillCredentials(tree: ReactTestRenderer.ReactTestRenderer) {
  await type(tree, 0, 'cr832');
  await type(tree, 1, '1234');
}

async function pickInto(tree: ReactTestRenderer.ReactTestRenderer, slot: 'Logo 1' | 'Logo 2', base64: string, mimeType: string) {
  mockLaunchImageLibrary.mockResolvedValueOnce({ assets: [{ base64, type: mimeType }] });
  // Both slots use the identically titled "Elegir imagen" button; index 0 is Logo 1, 1 is Logo 2.
  const buttons = tree.root.findAll((n) => typeof n.props?.onPress === 'function' && n.props?.title === 'Elegir imagen');
  await ReactTestRenderer.act(async () => {
    await buttons[slot === 'Logo 1' ? 0 : 1].props.onPress();
  });
}

describe('custom logo screen', () => {
  beforeEach(() => {
    mockSetDeviceLogos.mockReset().mockResolvedValue(OK);
    mockClearDeviceLogos.mockReset().mockResolvedValue(OK);
    mockRefreshSession.mockClear();
    mockLaunchImageLibrary.mockReset();
  });

  it('saves with only Logo 1 chosen, leaving Logo 2 untouched', async () => {
    const tree = await mount();
    await fillCredentials(tree);
    await pickInto(tree, 'Logo 1', 'portraitB', 'image/png');
    await press(tree, 'Guardar logos');

    // null for the slot that was not chosen -- the API layer turns that into an empty field, and
    // the stored procedure reads empty as "leave the stored one alone".
    expect(mockSetDeviceLogos).toHaveBeenCalledWith('cr832', '1234', { base64: 'portraitB', mimeType: 'image/png' }, null);
  });

  it('saves with only Logo 2 chosen', async () => {
    const tree = await mount();
    await fillCredentials(tree);
    await pickInto(tree, 'Logo 2', 'bannerB', 'image/jpeg');
    await press(tree, 'Guardar logos');

    expect(mockSetDeviceLogos).toHaveBeenCalledWith('cr832', '1234', null, { base64: 'bannerB', mimeType: 'image/jpeg' });
  });

  it('refuses to save when neither logo was chosen', async () => {
    const tree = await mount();
    await fillCredentials(tree);
    await press(tree, 'Guardar logos');

    expect(mockSetDeviceLogos).not.toHaveBeenCalled();
    expect(screenText(tree)).toContain('Elige al menos un logo primero.');
  });

  it('pulls the account down again after a save, so the change shows without logging in again', async () => {
    const tree = await mount();
    await fillCredentials(tree);
    await pickInto(tree, 'Logo 1', 'portraitB', 'image/png');
    await press(tree, 'Guardar logos');

    expect(mockRefreshSession).toHaveBeenCalledTimes(1);
    expect(screenText(tree)).toContain('Logos guardados.');
  });

  it('resets to the default images without needing a logo chosen', async () => {
    const tree = await mount();
    await fillCredentials(tree);
    await press(tree, 'Restablecer imágenes por defecto');

    expect(mockClearDeviceLogos).toHaveBeenCalledWith('cr832', '1234');
    expect(mockSetDeviceLogos).not.toHaveBeenCalled();
    expect(mockRefreshSession).toHaveBeenCalledTimes(1);
    expect(screenText(tree)).toContain('Imágenes por defecto restablecidas.');
  });

  it('still requires the panel credentials before resetting', async () => {
    const tree = await mount();
    await press(tree, 'Restablecer imágenes por defecto');

    expect(mockClearDeviceLogos).not.toHaveBeenCalled();
  });

  it('surfaces a server refusal instead of claiming success', async () => {
    mockSetDeviceLogos.mockResolvedValue({ State: 1, Message: 'Código de usuario incorrecto' });
    const tree = await mount();
    await fillCredentials(tree);
    await pickInto(tree, 'Logo 1', 'portraitB', 'image/png');
    await press(tree, 'Guardar logos');

    expect(mockRefreshSession).not.toHaveBeenCalled();
    expect(screenText(tree)).toContain('Código de usuario incorrecto');
  });
});

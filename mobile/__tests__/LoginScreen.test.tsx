/**
 * Covers client issue #21: after one failed login attempt the app refused the correct password
 * until it was force-closed.
 *
 * The screen used to clear the password box and force it to remount on every failure. On Android
 * that is the likely cause rather than the cure -- resetting a focused secureTextEntry input from
 * JS can leave the native view holding the old text, so the next onChangeText reports the old and
 * new text joined together and every later attempt is wrong whatever is typed. The fix is to stop
 * touching the field at all, which is what these tests pin.
 *
 * @format
 */

import React from 'react';
import ReactTestRenderer from 'react-test-renderer';
import { TextInput } from 'react-native';
import { LoginScreen } from '../src/screens/auth/LoginScreen';
import { ThemeProvider } from '../src/theme/ThemeProvider';
import { LocaleProvider } from '../src/i18n/LocaleContext';

const mockLogin = jest.fn();
jest.mock('../src/auth/AuthContext', () => ({
  useAuth: () => ({ login: mockLogin, session: null, logout: jest.fn() }),
}));

// LoginScreen clears a possibly-stuck `submitting` flag on focus (see its useFocusEffect). The
// real hook needs a NavigationContainer, which this screen is rendered outside of here, so stand
// it in with a plain mount effect -- same timing for this screen's purposes.
jest.mock('@react-navigation/native', () => {
  const actual = jest.requireActual('@react-navigation/native');
  const React = require('react');
  return {
    ...actual,
    useFocusEffect: (cb: React.EffectCallback) => React.useEffect(cb, [cb]),
  };
});

const navigation = { navigate: jest.fn(), replace: jest.fn() } as never;
const route = { params: undefined } as never;

function inputs(tree: ReactTestRenderer.ReactTestRenderer) {
  return tree.root.findAllByType(TextInput);
}

async function mount() {
  let tree!: ReactTestRenderer.ReactTestRenderer;
  await ReactTestRenderer.act(async () => {
    tree = ReactTestRenderer.create(
      <ThemeProvider>
        <LocaleProvider>
          <LoginScreen navigation={navigation} route={route} />
        </LocaleProvider>
      </ThemeProvider>,
    );
  });
  return tree;
}

async function type(tree: ReactTestRenderer.ReactTestRenderer, which: number, text: string) {
  await ReactTestRenderer.act(async () => {
    inputs(tree)[which].props.onChangeText(text);
  });
}

async function submit(tree: ReactTestRenderer.ReactTestRenderer) {
  const button = tree.root.findAll(
    (n) => typeof n.props?.onPress === 'function' && n.props?.title === 'Acceder',
  )[0];
  await ReactTestRenderer.act(async () => {
    await button.props.onPress();
  });
}

describe('login after a failed attempt (issue #21)', () => {
  beforeEach(() => {
    mockLogin.mockReset();
    (globalThis as { __resetAsyncStorage?: () => void }).__resetAsyncStorage?.();
  });

  it('keeps what the user typed after a wrong password, instead of clearing it', async () => {
    const tree = await mount();
    await type(tree, 0, 'user@example.com');
    await type(tree, 1, 'wrongpass');

    mockLogin.mockResolvedValueOnce({ ok: false, message: 'Login erróneo.' });
    await submit(tree);

    // The old code did setPassword('') here. Clearing it is what desynced the native input.
    expect(inputs(tree)[1].props.value).toBe('wrongpass');
  });

  it('sends exactly the corrected password on the next attempt, not the two joined together', async () => {
    const tree = await mount();
    await type(tree, 0, 'user@example.com');
    await type(tree, 1, 'wrongpass');

    mockLogin.mockResolvedValueOnce({ ok: false, message: 'Login erróneo.' });
    await submit(tree);

    // User corrects the field; onChangeText always carries the full new text.
    await type(tree, 1, 'correctpass');
    mockLogin.mockResolvedValueOnce({ ok: true, pinChanged: false, removedDevices: [] });
    await submit(tree);

    expect(mockLogin).toHaveBeenLastCalledWith('user@example.com', 'correctpass');
    expect(mockLogin).toHaveBeenCalledTimes(2);
  });

  it('does not remount the password field between attempts', async () => {
    const tree = await mount();
    await type(tree, 0, 'user@example.com');
    await type(tree, 1, 'wrongpass');
    const before = inputs(tree)[1].instance ?? inputs(tree)[1];

    mockLogin.mockResolvedValueOnce({ ok: false, message: 'Login erróneo.' });
    await submit(tree);

    // A changing `key` used to force a brand new native view here.
    const after = inputs(tree)[1].instance ?? inputs(tree)[1];
    expect(inputs(tree)).toHaveLength(2);
    expect(after).toBe(before);
  });

  it('clears the error as soon as the user edits a field', async () => {
    const tree = await mount();
    await type(tree, 0, 'user@example.com');
    await type(tree, 1, 'wrongpass');

    mockLogin.mockResolvedValueOnce({ ok: false, message: 'Login erróneo.' });
    await submit(tree);
    expect(JSON.stringify(tree.toJSON())).toContain('Login erróneo.');

    await type(tree, 1, 'wrongpas');
    expect(JSON.stringify(tree.toJSON())).not.toContain('Login erróneo.');
  });
});

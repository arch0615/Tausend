/**
 * Covers the "is the panel actually reachable" decision behind the connection indicator.
 *
 * The first attempt at the #2/#16 fix got this wrong: it treated any response object as proof
 * of life, so a panel that had dropped off the server still reported as connected. These cases
 * pin the distinction between "the request completed" and "the panel replied".
 *
 * @format
 */

import { panelAnswered } from '../src/panels/commandFeedback';
import { ResponseState } from '../src/api/types';

describe('panelAnswered', () => {
  it('is true for a real reply', () => {
    expect(panelAnswered({ State: ResponseState.OK, Text: 'READY' })).toBe(true);
  });

  it('is false when the backend says the panel never responded', () => {
    // The exact case that kept the indicator on "Conectada": HTTP succeeded, panel did not reply.
    expect(panelAnswered({ State: ResponseState.CENTRAL_UNRESPONSIVE, Text: 'anything' })).toBe(false);
  });

  it('is false for an empty reply, which the relay sends when nothing came back', () => {
    expect(panelAnswered({ State: ResponseState.OK, Text: '' })).toBe(false);
    expect(panelAnswered({ State: ResponseState.OK, Text: '   ' })).toBe(false);
  });

  it('is false when Text is missing entirely', () => {
    expect(panelAnswered({ State: ResponseState.OK })).toBe(false);
    expect(panelAnswered({ State: ResponseState.OK, Text: null })).toBe(false);
  });
});

import { ApiTimeoutError } from '../api/client';
import { ResponseState, type BaseResponse } from '../api/types';

// Every command screen used to collapse every non-OK outcome into one generic "the panel did
// not respond" string -- offline, wrong PIN, business rejection, and a genuine timeout all
// looked identical to the user. This maps the backend's already-distinct ResponseState values
// (and a network/timeout failure, which never reaches ResponseState at all) to a specific
// message per case, so screens using it can finally tell these apart.
export function describeCommandFailure(res: BaseResponse, t: (key: string, vars?: Record<string, string>) => string): string {
  switch (res.State) {
    case ResponseState.CENTRAL_UNRESPONSIVE:
      return t("The panel isn't responding -- it may be offline.");
    case ResponseState.WRONG_DATA:
      return res.Message || t('The panel rejected that PIN or value.');
    case ResponseState.BUSSINESS_ERROR:
      return res.Message || t('The panel could not complete that action right now.');
    case ResponseState.FORBIDDEN:
      return t('Your account is not authorized for that action.');
    case ResponseState.NOT_FOUND:
      return t('This panel could not be found.');
    case ResponseState.SERVER_ERROR:
    default:
      return res.Message || t('Something went wrong. Try again.');
  }
}

/** For the catch block around a command call -- distinguishes a real timeout from a plain
 * network/DNS failure, which describeCommandFailure can't do since neither ever produces a
 * ResponseState at all. */
export function describeCommandException(e: unknown, t: (key: string) => string): string {
  if (e instanceof ApiTimeoutError) {
    return t('The panel took too long to respond. Try again.');
  }
  return t('Could not reach the server.');
}

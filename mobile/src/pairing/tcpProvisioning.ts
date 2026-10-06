import TcpSocket from 'react-native-tcp-socket';
import type Socket from 'react-native-tcp-socket/lib/types/Socket';

// Panel-local pairing protocol, ported from the previous (Ionic) app's working implementation
// (src/app/tcp/tcp.page.ts) -- not documented in the panel's own manufacturer spec sheets
// available in this repo. When the panel is in Access Point mode it listens on a fixed local
// IP/port, accepts plain (unencrypted) UTF-8 text commands terminated by a single carriage
// return, and echoes back whatever it receives (or "ERROR" for a command it doesn't recognize)
// -- the previous app's "Access Point" screen displayed that echo live as a debug console:
//   PRG350:<ssid>\r      -- set the home Wi-Fi network name the panel should join
//   PRG351:<password>\r  -- set the home Wi-Fi password
// Sending both back-to-back too fast can lose the second one (empirically load-bearing in the
// reference implementation) -- a fixed delay between them is kept here.
export const DEFAULT_PANEL_IP = '192.168.4.1';
export const DEFAULT_PANEL_PORT = 8002;

const INTER_COMMAND_DELAY_MS = 200;
const CONNECT_TIMEOUT_MS = 6000;

export type ConnectError = 'connect_failed' | 'connect_timeout';
export type ConnectResult = { ok: true; socket: Socket } | { ok: false; error: ConnectError };

// Opens a socket to the panel and leaves it open -- unlike a one-shot request/response call, the
// previous app's Access Point screen keeps this connection alive for the whole session so the
// installer can send the Wi-Fi credentials, then any number of raw commands, before finishing.
export function connectPanelSocket(host: string, port: number): Promise<ConnectResult> {
  return new Promise((resolve) => {
    let settled = false;
    let socket: Socket | null = null;

    function settle(result: ConnectResult) {
      if (settled) return;
      settled = true;
      resolve(result);
    }

    socket = TcpSocket.createConnection(
      {
        host,
        port,
        // The panel's AP has no internet -- force this connection over the Wi-Fi interface so
        // the OS doesn't route it over cellular data instead (which could never reach a private
        // 192.168.x.x address anyway, but would otherwise surface as a confusing generic failure).
        interface: 'wifi',
        connectTimeout: CONNECT_TIMEOUT_MS,
      },
      () => settle({ ok: true, socket: socket as Socket }),
    );

    socket.on('error', () => settle({ ok: false, error: 'connect_failed' }));
    socket.on('timeout', () => settle({ ok: false, error: 'connect_timeout' }));
  });
}

export function writePanelCommand(socket: Socket, text: string): Promise<void> {
  return new Promise((resolve, reject) => {
    socket.write(`${text}\r`, 'utf8', (err) => (err ? reject(err) : resolve()));
  });
}

export async function sendWifiCredentials(socket: Socket, ssid: string, password: string): Promise<void> {
  await writePanelCommand(socket, `PRG350:${ssid}`);
  await delay(INTER_COMMAND_DELAY_MS);
  await writePanelCommand(socket, `PRG351:${password}`);
}

function delay(ms: number): Promise<void> {
  return new Promise((resolve) => setTimeout(resolve, ms));
}

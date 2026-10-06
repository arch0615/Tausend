import { useCallback, useEffect, useRef, useState } from 'react';
import {
  ActivityIndicator,
  Alert,
  FlatList,
  KeyboardAvoidingView,
  Modal,
  Platform,
  Pressable,
  ScrollView,
  StyleSheet,
  Text,
  View,
} from 'react-native';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import type Socket from 'react-native-tcp-socket/lib/types/Socket';
import { useTheme } from '../../theme/ThemeProvider';
import { GradientBackground } from '../../components/GradientBackground';
import { Card } from '../../components/Card';
import { useLocale } from '../../i18n/LocaleContext';
import { FormField } from '../../components/FormField';
import { PrimaryButton } from '../../components/PrimaryButton';
import { Banner } from '../../components/Banner';
import { LoadingOverlay } from '../../components/LoadingOverlay';
import {
  connectToPanelAp,
  getCurrentSsid,
  requestWifiScanPermission,
  scanForNetworks,
  type ScannedNetwork,
  type WifiConnectError,
} from '../../pairing/wifi';
import {
  DEFAULT_PANEL_IP,
  DEFAULT_PANEL_PORT,
  connectPanelSocket,
  sendWifiCredentials,
  writePanelCommand,
  type ConnectError,
} from '../../pairing/tcpProvisioning';
import type { MainStackParamList } from '../../navigation/types';

type Props = NativeStackScreenProps<MainStackParamList, 'ConnectToPanel'>;

const DEFAULT_INSTALLER_PASSWORD = '88888888';

function joinErrorMessage(error: WifiConnectError, t: ReturnType<typeof useLocale>['t']): string {
  const messages: Record<WifiConnectError, string> = {
    wrong_password: t('Incorrect password for that network -- check the installer code and try again.'),
    not_found: t("Couldn't find that network. Make sure the panel is still in setup mode and try again."),
    timeout: t('Connection timed out. Move closer to the panel and try again.'),
    permission_denied: t('Location permission is required to connect to a Wi-Fi network.'),
    unknown: t('Could not connect to that network. Try again.'),
  };
  return messages[error];
}

function tcpErrorMessage(error: ConnectError, t: ReturnType<typeof useLocale>['t']): string {
  const messages: Record<ConnectError, string> = {
    connect_failed: t(
      "Couldn't reach the panel. Make sure your phone is still connected to its network and try again.",
    ),
    connect_timeout: t('Connecting to the panel timed out. Move closer to it and try again.'),
  };
  return messages[error];
}

// Ported from the previous (Ionic) app's src/app/tcp/tcp.page.ts + .html -- the video
// (New APP Videos/Access Point.mp4, 12 frames) only shows the visuals; this file is the actual
// source of the flow, per memory/project_frontend_design_philosophy.md. Two distinct "connected"
// states exist and were previously conflated: joining the panel's own Wi-Fi AP (Card 1) is
// separate from opening the TCP session to it (Card 2) -- the previous version of this screen
// treated Card 1's join as the only step and navigated to a different screen entirely for what
// is actually Card 2 morphing in place into a live command console once the socket opens.
export function ConnectToPanelScreen({ navigation }: Props) {
  const { colors, typography, spacing } = useTheme();
  const { t } = useLocale();
  const isAndroid = Platform.OS === 'android';

  const [view, setView] = useState<'main' | 'networks'>('main');
  const [currentSsid, setCurrentSsid] = useState<string | null>(null);
  // Snapshot of the phone's own network at mount, before it ever joins the panel's setup AP --
  // currentSsid gets overwritten to the panel's own AP name once joined (see handleConfirmJoin),
  // so this is the only thing that still knows the home network's name by the time the Wi-Fi
  // credentials modal opens.
  const homeSsidRef = useRef<string | null>(null);

  const [networks, setNetworks] = useState<ScannedNetwork[]>([]);
  const [scanning, setScanning] = useState(false);
  const [scanError, setScanError] = useState<string | null>(null);

  const [joinTarget, setJoinTarget] = useState<ScannedNetwork | null>(null);
  const [joinPassword, setJoinPassword] = useState(DEFAULT_INSTALLER_PASSWORD);
  const [joining, setJoining] = useState(false);
  const [joinError, setJoinError] = useState<string | null>(null);
  const [joinedBanner, setJoinedBanner] = useState<string | null>(null);

  const [ip, setIp] = useState(DEFAULT_PANEL_IP);
  const [port, setPort] = useState(String(DEFAULT_PANEL_PORT));
  const [tcpConnecting, setTcpConnecting] = useState(false);
  const [tcpError, setTcpError] = useState<string | null>(null);
  const [tcpConnected, setTcpConnected] = useState(false);
  const socketRef = useRef<Socket | null>(null);

  const [log, setLog] = useState<string[]>([]);
  const [command, setCommand] = useState('');
  const [sendingCommand, setSendingCommand] = useState(false);

  const [wifiModalVisible, setWifiModalVisible] = useState(false);
  const [wifiName, setWifiName] = useState('');
  const [wifiPassword, setWifiPassword] = useState('');
  const [wifiModalError, setWifiModalError] = useState<string | null>(null);
  const [sendingWifiCreds, setSendingWifiCreds] = useState(false);

  const refreshCurrentSsid = useCallback(async () => {
    // Reading the current SSID needs location permission on Android just like scanning does --
    // without requesting it first, getCurrentSsid() silently returns null and the button shows
    // "No network" even on a phone that's genuinely connected, until something else happens to
    // trigger the permission prompt first.
    if (isAndroid) await requestWifiScanPermission();
    const ssid = await getCurrentSsid();
    setCurrentSsid(ssid);
    return ssid;
  }, [isAndroid]);

  useEffect(() => {
    refreshCurrentSsid().then((ssid) => {
      if (homeSsidRef.current === null) homeSsidRef.current = ssid;
    });
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  useEffect(() => {
    return () => {
      socketRef.current?.destroy();
    };
  }, []);

  async function handleWifiButton() {
    if (!isAndroid) {
      Alert.alert(t('ACCESS POINT'), t("Connect to the panel's network from your phone's Wi-Fi settings."));
      return;
    }
    setScanError(null);
    setView('networks');
    setScanning(true);
    try {
      const granted = await requestWifiScanPermission();
      if (!granted) {
        setScanError(t('Location permission is required to scan for Wi-Fi networks.'));
        return;
      }
      setNetworks(await scanForNetworks());
    } catch {
      setScanError(t('Could not scan for Wi-Fi networks.'));
    } finally {
      setScanning(false);
    }
  }

  function handleTapNetwork(network: ScannedNetwork) {
    setJoinTarget(network);
    setJoinPassword(DEFAULT_INSTALLER_PASSWORD);
    setJoinError(null);
  }

  async function handleConfirmJoin() {
    if (!joinTarget) return;
    setJoining(true);
    setJoinError(null);
    try {
      const res = await connectToPanelAp(joinTarget.ssid, joinPassword);
      if (res.ok) {
        setJoinedBanner(t('Connected to {ssid}', { ssid: joinTarget.ssid }));
        setJoinTarget(null);
        refreshCurrentSsid();
        setView('main');
      } else {
        setJoinError(joinErrorMessage(res.error, t));
      }
    } catch {
      setJoinError(joinErrorMessage('unknown', t));
    } finally {
      setJoining(false);
    }
  }

  async function handleTcpConnect() {
    setTcpError(null);
    setTcpConnecting(true);
    try {
      const res = await connectPanelSocket(ip.trim() || DEFAULT_PANEL_IP, Number(port) || DEFAULT_PANEL_PORT);
      if (res.ok) {
        res.socket.setEncoding('utf8');
        res.socket.on('data', (data) => {
          const text = typeof data === 'string' ? data : data.toString('utf8');
          setLog((prev) => [text, ...prev]);
        });
        res.socket.on('close', () => {
          setTcpConnected(false);
          socketRef.current = null;
        });
        socketRef.current = res.socket;
        setTcpConnected(true);
      } else {
        setTcpError(tcpErrorMessage(res.error, t));
      }
    } catch {
      setTcpError(tcpErrorMessage('connect_failed', t));
    } finally {
      setTcpConnecting(false);
    }
  }

  async function handleSendCommand() {
    if (!command.trim() || !socketRef.current) return;
    setSendingCommand(true);
    setTcpError(null);
    try {
      await writePanelCommand(socketRef.current, command.trim());
      setCommand('');
    } catch {
      setTcpError(t('Lost the connection to the panel.'));
      setTcpConnected(false);
    } finally {
      setSendingCommand(false);
    }
  }

  function openWifiCredsModal() {
    setWifiModalError(null);
    // Prefill with the home network the phone was on before joining the panel's setup AP --
    // still fully editable in case that guess is wrong, but saves retyping it in the common
    // case. Only prefills once (doesn't clobber a value the installer already typed/edited).
    if (!wifiName && homeSsidRef.current) setWifiName(homeSsidRef.current);
    setWifiModalVisible(true);
  }

  async function handleConfirmWifiCreds() {
    if (!wifiName.trim() || !wifiPassword.trim()) {
      setWifiModalError(t('Please fill in both fields.'));
      return;
    }
    if (!socketRef.current) {
      setWifiModalError(t('Lost the connection to the panel.'));
      return;
    }
    setSendingWifiCreds(true);
    setWifiModalError(null);
    try {
      await sendWifiCredentials(socketRef.current, wifiName.trim(), wifiPassword.trim());
      setWifiModalVisible(false);
    } catch {
      setWifiModalError(t('Could not send the command to the panel.'));
    } finally {
      setSendingWifiCreds(false);
    }
  }

  function handleFinish() {
    Alert.alert(t('Finish Access Point'), t(
      "To finish, take the panel out of Access Point mode. Wait a few seconds for it to connect to the server -- once it does, you can link the device to your account. If it doesn't connect, put the panel back into Access Point mode and double-check the network name and password.",
    ), [
      {
        text: t('OK'),
        onPress: () => {
          socketRef.current?.destroy();
          navigation.navigate('Home');
        },
      },
    ]);
  }

  if (view === 'networks') {
    return (
      <GradientBackground style={{ flex: 1 }}>
        <ScrollView contentContainerStyle={styles.container}>
          <Card style={styles.card}>
            <Text style={[typography.title, { color: colors.ink, textAlign: 'center', marginBottom: spacing.md }]}>
              {t('Network selector')}
            </Text>
            {scanError && <Banner kind="error">{scanError}</Banner>}
            {scanning ? (
              <ActivityIndicator style={{ marginVertical: 24 }} size="large" color={colors.accent} />
            ) : networks.length === 0 ? (
              <Text style={[typography.bodyDim, { color: colors.inkDim, textAlign: 'center' }]}>
                {t('No networks available.')}
              </Text>
            ) : (
              <FlatList
                data={networks}
                keyExtractor={(n) => n.bssid || n.ssid}
                scrollEnabled={false}
                renderItem={({ item }) => (
                  <Pressable
                    onPress={() => handleTapNetwork(item)}
                    style={[styles.networkRow, { backgroundColor: colors.headerBg }]}
                  >
                    <Text style={{ fontSize: 18, marginRight: 10 }}>{'\u{1F4F6}'}</Text>
                    <Text style={[typography.body, { color: colors.accentInk, flex: 1 }]} numberOfLines={1}>
                      {item.ssid} {item.frequency} MHz
                    </Text>
                  </Pressable>
                )}
              />
            )}
          </Card>
          <View style={{ marginTop: spacing.lg }}>
            <PrimaryButton title={t('Back')} onPress={() => setView('main')} tone="outline" />
          </View>
        </ScrollView>

        <Modal visible={joinTarget !== null} transparent animationType="fade" onRequestClose={() => setJoinTarget(null)}>
          <View style={styles.modalBackdrop}>
            <Card style={styles.modalCard}>
              <Text style={[typography.title, { color: colors.ink, marginBottom: spacing.sm }]}>
                {t('Connect to {ssid}', { ssid: joinTarget?.ssid ?? '' })}
              </Text>
              <Text style={[typography.bodyDim, { color: colors.inkDim, marginBottom: spacing.md }]}>
                {t('If you changed the installer code, replace the default value.')}
              </Text>
              {joinError && <Banner kind="error">{joinError}</Banner>}
              <FormField label={t('Password')} value={joinPassword} onChangeText={setJoinPassword} secureTextEntry />
              <View style={styles.modalButtonRow}>
                <View style={{ flex: 1, marginRight: spacing.sm }}>
                  <PrimaryButton title={t('Cancel')} onPress={() => setJoinTarget(null)} tone="outline" disabled={joining} />
                </View>
                <View style={{ flex: 1 }}>
                  <PrimaryButton title={t('Confirm')} onPress={handleConfirmJoin} loading={joining} />
                </View>
              </View>
            </Card>
          </View>
        </Modal>
        <LoadingOverlay visible={joining} label={t('Please wait')} />
      </GradientBackground>
    );
  }

  return (
    <>
    <GradientBackground style={{ flex: 1 }}>
    <KeyboardAvoidingView style={{ flex: 1 }} behavior={Platform.OS === 'ios' ? 'padding' : 'height'}>
      <ScrollView contentContainerStyle={styles.container} keyboardShouldPersistTaps="handled">
        {joinedBanner && <Banner kind="ok">{joinedBanner}</Banner>}

        <Card style={styles.card}>
          <Text style={[typography.bodyDim, { color: colors.inkDim, textAlign: 'center', marginBottom: spacing.md }]}>
            {t('Put the panel into Access Point mode, find its network, and connect.')}
          </Text>
          {isAndroid && (
            <Pressable onPress={handleWifiButton} style={[styles.wifiButton, { backgroundColor: colors.headerBg }]}>
              <Text style={{ fontSize: 18, marginRight: 10 }}>{'\u{1F4F6}'}</Text>
              <Text style={[typography.body, { color: colors.accentInk, fontWeight: '700' }]} numberOfLines={1}>
                {currentSsid ?? t('No network')}
              </Text>
            </Pressable>
          )}
        </Card>

        {!tcpConnected ? (
          <Card style={[styles.card, { marginTop: spacing.lg }]}>
            <Text style={[typography.bodyDim, { color: colors.inkDim, textAlign: 'center', marginBottom: spacing.md }]}>
              {t('Once your phone is connected to the panel network, connect the app to the panel.')}
            </Text>
            {tcpError && <Banner kind="error">{tcpError}</Banner>}
            <FormField label={t('Remote IP address')} value={ip} onChangeText={setIp} autoCapitalize="none" autoCorrect={false} style={styles.underlineField} />
            <FormField label={t('Remote port')} value={port} onChangeText={setPort} keyboardType="number-pad" style={styles.underlineField} />
            <View style={{ marginTop: spacing.md }}>
              <PrimaryButton title={t('Connect')} onPress={handleTcpConnect} disabled={tcpConnecting} tone="dark" />
            </View>
          </Card>
        ) : (
          <Card style={[styles.card, { marginTop: spacing.lg }]}>
            {tcpError && <Banner kind="error">{tcpError}</Banner>}
            <ScrollView style={[styles.logBox, { borderColor: colors.accent }]} nestedScrollEnabled>
              {log.map((line, i) => (
                <Text key={i} style={styles.logLine}>{line}</Text>
              ))}
            </ScrollView>
            <View style={styles.commandRow}>
              <View style={{ flex: 1, marginRight: spacing.sm }}>
                <FormField label="Command" value={command} onChangeText={setCommand} autoCapitalize="none" autoCorrect={false} />
              </View>
              <Pressable
                onPress={handleSendCommand}
                disabled={sendingCommand || !command.trim()}
                style={[styles.sendButton, { backgroundColor: colors.headerBg, opacity: sendingCommand || !command.trim() ? 0.5 : 1 }]}
              >
                <Text style={{ fontSize: 18, color: colors.accentInk }}>{'\u{27A4}'}</Text>
              </Pressable>
            </View>
            <View style={styles.modalButtonRow}>
              <View style={{ flex: 1, marginRight: spacing.sm }}>
                <PrimaryButton title={t('Configure network name and password')} onPress={openWifiCredsModal} tone="dark" />
              </View>
              <View style={{ flex: 1 }}>
                <PrimaryButton title={t('Finish')} onPress={handleFinish} />
              </View>
            </View>
          </Card>
        )}

        <View style={{ marginTop: spacing.xl, alignSelf: 'center', width: '60%' }}>
          <PrimaryButton title={t('Home')} onPress={() => navigation.navigate('Home')} />
        </View>
      </ScrollView>
    </KeyboardAvoidingView>
    </GradientBackground>

    <Modal visible={wifiModalVisible} transparent animationType="fade" onRequestClose={() => setWifiModalVisible(false)}>
      <View style={styles.modalBackdrop}>
        <Card style={styles.modalCard}>
          <Text style={[typography.title, { color: colors.ink, marginBottom: spacing.md }]}>
            {t('Configure network name and password')}
          </Text>
          {wifiModalError && <Banner kind="error">{wifiModalError}</Banner>}
          <FormField label={t('Network name - PRG350')} value={wifiName} onChangeText={setWifiName} autoCapitalize="none" autoCorrect={false} />
          <FormField label={t('Password - PRG351')} value={wifiPassword} onChangeText={setWifiPassword} />
          <View style={styles.modalButtonRow}>
            <View style={{ flex: 1, marginRight: spacing.sm }}>
              <PrimaryButton title={t('Cancel')} onPress={() => setWifiModalVisible(false)} tone="outline" disabled={sendingWifiCreds} />
            </View>
            <View style={{ flex: 1 }}>
              <PrimaryButton title={t('Confirm')} onPress={handleConfirmWifiCreds} loading={sendingWifiCreds} />
            </View>
          </View>
        </Card>
      </View>
    </Modal>
    <LoadingOverlay visible={tcpConnecting} label={t('Please wait')} />
    </>
  );
}

const styles = StyleSheet.create({
  container: {
    flexGrow: 1,
    padding: 24,
  },
  card: {
    alignItems: 'stretch',
  },
  // Wide pill icon button, matching the reference's dark teal Wi-Fi button -- labeled with the
  // phone's current SSID, same as the previous app's currentSSID readout.
  wifiButton: {
    height: 48,
    borderRadius: 24,
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    paddingHorizontal: 16,
  },
  networkRow: {
    height: 48,
    borderRadius: 24,
    flexDirection: 'row',
    alignItems: 'center',
    paddingHorizontal: 16,
    marginBottom: 10,
  },
  // The reference shows "Dirección IP Remota"/"Puerto Remoto" as underlined fields rather than
  // FormField's usual bordered box -- FormField's own default style is left alone everywhere else.
  underlineField: {
    borderWidth: 0,
    borderBottomWidth: 1,
    borderRadius: 0,
    paddingHorizontal: 0,
  },
  logBox: {
    maxHeight: 140,
    borderWidth: 1,
    borderRadius: 10,
    padding: 12,
    marginBottom: 12,
  },
  logLine: {
    color: '#2f9e44',
    fontWeight: '700',
    marginBottom: 12,
  },
  commandRow: {
    flexDirection: 'row',
    alignItems: 'flex-end',
  },
  sendButton: {
    width: 44,
    height: 44,
    borderRadius: 10,
    alignItems: 'center',
    justifyContent: 'center',
    marginBottom: 20,
  },
  modalButtonRow: {
    flexDirection: 'row',
    marginTop: 8,
  },
  modalBackdrop: {
    flex: 1,
    alignItems: 'center',
    justifyContent: 'center',
    backgroundColor: 'rgba(0,0,0,0.35)',
    padding: 24,
  },
  modalCard: {
    width: '100%',
    maxWidth: 360,
  },
});

import { useCallback, useEffect, useLayoutEffect, useRef, useState } from 'react';
import { ActivityIndicator, Alert, Animated, Image, Pressable, ScrollView, StyleSheet, Text, View } from 'react-native';
import { useFocusEffect } from '@react-navigation/native';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import { useTheme } from '../../theme/ThemeProvider';
import { GradientBackground } from '../../components/GradientBackground';
import { Card } from '../../components/Card';
import { useLocale } from '../../i18n/LocaleContext';
import { useAuth } from '../../auth/AuthContext';
import { usePanels } from '../../panels/PanelContext';
import {
  armAlarm,
  dayArmAlarm,
  disarmAlarm,
  duress,
  emergency,
  getGeneralStatus,
  nightArmAlarm,
  panic,
} from '../../api/command';
import { ResponseState, type CommandResponse } from '../../api/types';
import { STATUS_TOKEN_MESSAGE_KEY, parseStatus, statusTokens, type ArmMode, type ParsedStatus } from '../../panels/statusParser';
import { describeCommandException, describeCommandFailure } from '../../panels/commandFeedback';
import { SmsCommands } from '../../pairing/smsCommands';
import { openSmsComposer } from '../../pairing/smsIntent';
import { PrimaryButton } from '../../components/PrimaryButton';
import { Banner } from '../../components/Banner';
import { StatusDot } from '../../components/StatusDot';
import { LoadingOverlay } from '../../components/LoadingOverlay';
import { Snackbar } from '../../components/Snackbar';
import { MainMenu, MenuButton } from '../../components/MainMenu';
import { shareEmergencyLocation } from '../../panels/emergencyLocation';
import type { MainStackParamList } from '../../navigation/types';

// Client-supplied 3D icons ("Pruebas 22-9-26" feedback round) to replace the plain OS emoji
// previously used for these two ActionCards -- rendering varied a lot across Android skins
// (e.g. MIUI's closed-lock emoji reading more like a keyhole than the Asalto Silencioso card's
// intended safe/vault meaning).
const PANIC_ICON = require('../../assets/images/icon-panic-bell.png');
const DURESS_ICON = require('../../assets/images/icon-duress-safe.png');

type Props = NativeStackScreenProps<MainStackParamList, 'Home'>;

// Ported from the previous app's polling loop (home.page.ts): up to 4 attempts total (the
// original's "retrys 0..3"), 200ms apart, before giving up and showing "not responding".
const STATUS_MAX_ATTEMPTS = 4;
const STATUS_RETRY_DELAY_MS = 200;
const POLL_INTERVAL_MS = 30000;

function delay(ms: number): Promise<void> {
  return new Promise((resolve) => setTimeout(resolve, ms));
}

type HomeNavigation = Props['navigation'];

// Same asset as the login screen's wordmark (see LoginScreen.tsx) -- New APP Videos/'s equivalent
// "no panel yet" screen (Vincular Equipo) also centers the eagle above the message, on a white
// card floating on the gradient, not text placed directly on the gradient itself.
const EAGLE_LOGO = require('../../assets/images/eagle-logo.png');

export function HomeScreen({ navigation }: Props) {
  const { colors, typography, spacing } = useTheme();
  const { t } = useLocale();
  const { panels, selected } = usePanels();

  // Access Point.mp4's "VINCULAR EQUIPO" screen (frames 1-2, the zero-panels state) keeps the
  // normal teal header -- only the populated dashboard (IpHome/SmsHome) hides it in favor of the
  // inline TopBar below. MainStack.tsx sets headerShown:false for the whole Home route by
  // default, so it's turned back on here for every state except the dashboard.
  useLayoutEffect(() => {
    navigation.setOptions({
      headerShown: !selected,
      title: panels.length === 0 ? t('VINCULAR EQUIPO') : t('Home'),
    });
  }, [navigation, selected, panels.length, t]);

  if (!selected) {
    // Ported from the previous app's login redirect (login.page.ts: zero Devices AND zero
    // SMSDevices sends the user straight to the pairing flow instead of an empty Home/selector).
    // Access Point.mp4's "VINCULAR EQUIPO" screen for this exact state (frame 1) shows a plain
    // card with no logo -- "No existen centrales vinculadas", then Añadir Equipo (dark teal) and
    // Conectar Access Point (green) -- not the eagle+"Welcome" card this used to show, and not
    // the same fill color for both buttons.
    if (panels.length === 0) {
      return (
        <GradientBackground style={styles.centered}>
          <Card style={styles.emptyCard}>
            <Text style={[typography.body, { color: colors.ink, marginBottom: spacing.xl, textAlign: 'center' }]}>
              {t('No linked panels')}
            </Text>
            <PrimaryButton title={t('Add equipment')} onPress={() => navigation.navigate('SelectPairingType')} tone="dark" />
            <View style={{ height: spacing.md }} />
            <PrimaryButton title={t('Connect Access Point')} onPress={() => navigation.navigate('PairingIntro')} />
          </Card>
        </GradientBackground>
      );
    }
    return (
      <GradientBackground style={styles.centered}>
        <Card style={styles.emptyCard}>
          <Image source={EAGLE_LOGO} style={styles.emptyLogo} resizeMode="contain" />
          <Text style={[typography.title, { color: colors.ink, marginBottom: spacing.xs, textAlign: 'center' }]}>
            {t('Home')}
          </Text>
          <Text style={[typography.bodyDim, { color: colors.inkDim, marginBottom: spacing.xl, textAlign: 'center' }]}>
            {t('Select a panel to get started.')}
          </Text>
          <PrimaryButton title={t('Switch panel')} onPress={() => navigation.navigate('PanelSelector')} />
        </Card>
      </GradientBackground>
    );
  }

  return selected.kind === 'sms' ? (
    <SmsHome navigation={navigation} />
  ) : (
    <IpHome
      navigation={navigation}
      deviceId={selected.deviceId}
      description={selected.description}
      isOnline={selected.isOnline}
    />
  );
}

function IpHome({
  navigation,
  deviceId,
  description,
  isOnline,
}: {
  navigation: HomeNavigation;
  deviceId: number;
  description: string;
  isOnline?: boolean;
}) {
  const { colors, typography, spacing, radius } = useTheme();
  const { t } = useLocale();
  const { logout } = useAuth();
  const { reachabilityOf, reportReachability } = usePanels();
  const [status, setStatus] = useState<ParsedStatus | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [armExpanded, setArmExpanded] = useState(false);
  const [actionPending, setActionPending] = useState(false);
  const [actionToast, setActionToast] = useState<string | null>(null);
  const [triggerPending, setTriggerPending] = useState<'panic' | 'duress' | 'emergency' | null>(null);
  const [menuOpen, setMenuOpen] = useState(false);
  const deviceIdRef = useRef(deviceId);
  deviceIdRef.current = deviceId;

  const fetchStatus = useCallback(async () => {
    setLoading(true);
    let lastResponse: Awaited<ReturnType<typeof getGeneralStatus>> | null = null;
    let lastException: unknown = null;
    for (let attempt = 0; attempt < STATUS_MAX_ATTEMPTS; attempt++) {
      try {
        const res = await getGeneralStatus(deviceIdRef.current);
        if (res.State === ResponseState.UNAUTHORIZED || res.Code === 401) {
          // No manual navigation needed -- RootNavigator swaps to the login stack as soon as
          // session clears (see navigation/RootNavigator.tsx).
          logout();
          return;
        }
        lastResponse = res;
        lastException = null;
        const noResponse = res.State === ResponseState.CENTRAL_UNRESPONSIVE || !res.Text || res.Text.trim() === '';
        if (!noResponse) break;
      } catch (e) {
        lastResponse = null;
        lastException = e;
      }
      if (attempt < STATUS_MAX_ATTEMPTS - 1) await delay(STATUS_RETRY_DELAY_MS);
    }
    setLoading(false);
    if (!lastResponse) {
      setError(lastException ? describeCommandException(lastException, t) : t("The panel isn't responding -- it may be offline."));
      setStatus(null);
      // Every retry above is exhausted at this point, so this is a real "cannot reach it", not a
      // single dropped packet. See PanelContext.reportReachability for why the connection
      // indicator can't just keep trusting the login-time flag.
      reportReachability('ip', deviceIdRef.current, false);
      return;
    }
    setError(null);
    setStatus(parseStatus(lastResponse.Text ?? ''));
    // A parseable reply is proof the panel is reachable right now.
    reportReachability('ip', deviceIdRef.current, true);
  }, [logout, t, reportReachability]);

  useFocusEffect(
    useCallback(() => {
      let cancelled = false;
      let interval: ReturnType<typeof setInterval> | undefined;

      (async () => {
        await fetchStatus();
        if (cancelled) return;
        interval = setInterval(fetchStatus, POLL_INTERVAL_MS);
      })();

      return () => {
        cancelled = true;
        if (interval) clearInterval(interval);
        setArmExpanded(false);
      };
      // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [deviceId]),
  );

  async function runAction(action: () => Promise<CommandResponse>) {
    setArmExpanded(false);
    setActionPending(true);
    setActionToast(null);
    try {
      const res = await action();
      if (res.State === ResponseState.UNAUTHORIZED || res.Code === 401) {
        logout();
        return;
      }
      if (res.State === ResponseState.OK && res.Text && res.Text !== 'ERROR') {
        setStatus(parseStatus(res.Text));
        // Ported from the previous app's getStatusMsg(): every token in the response joined into
        // one human-readable confirmation, e.g. "Armed, Armed (away mode), Armed with no entry
        // delay" -- shown as a dismissible bottom toast, not folded into the status card itself.
        const message = statusTokens(res.Text)
          .map((token) => t(STATUS_TOKEN_MESSAGE_KEY[token] ?? token))
          .join(', ');
        if (message) setActionToast(message);
      } else {
        setError(describeCommandFailure(res, t));
      }
    } catch (e) {
      setError(describeCommandException(e, t));
    } finally {
      setActionPending(false);
    }
  }

  // Client requirement: emergency actions must avoid accidental activation and give
  // "extremely clear" success/failure feedback -- a blocking Alert for both the confirmation
  // and the result, not the same low-visibility Banner used for routine info everywhere else
  // on this screen. This intentionally replaces the previous "no confirmation, it's an
  // emergency button" design -- the client's explicit requirement supersedes that tradeoff.
  const TRIGGER_CONFIRM: Record<'panic' | 'duress' | 'emergency', string> = {
    panic: t('Send a panic alert? This notifies your emergency contacts immediately.'),
    duress: t('Send a duress alert? Only use this under threat or coercion.'),
    emergency: t('Send a medical emergency alert? This notifies your emergency contacts immediately.'),
  };

  const TRIGGER_SENT_TITLE: Record<'panic' | 'duress' | 'emergency', string> = {
    panic: t('Panic alert sent'),
    duress: t('Duress alert sent'),
    emergency: t('Emergency alert sent'),
  };

  const TRIGGER_FAILED_TITLE: Record<'panic' | 'duress' | 'emergency', string> = {
    panic: t('Panic alert failed'),
    duress: t('Duress alert failed'),
    emergency: t('Emergency alert failed'),
  };

  function confirmTrigger(kind: 'panic' | 'duress' | 'emergency', action: () => Promise<CommandResponse>) {
    Alert.alert(TRIGGER_CONFIRM[kind], undefined, [
      { text: t('Cancel'), style: 'cancel' },
      { text: t('Send'), style: 'destructive', onPress: () => runTrigger(kind, action) },
    ]);
  }

  async function runTrigger(kind: 'panic' | 'duress' | 'emergency', action: () => Promise<CommandResponse>) {
    setTriggerPending(kind);
    try {
      const res = await action();
      if (res.State === ResponseState.UNAUTHORIZED || res.Code === 401) {
        logout();
        return;
      }
      if (res.State === ResponseState.OK && res.Text !== 'ERROR') {
        Alert.alert(TRIGGER_SENT_TITLE[kind], t('The panel confirmed receipt.'));
        // Fire-and-forget -- never let a location/share failure affect the already-sent command
        // or its own success feedback above.
        shareEmergencyLocation(`${TRIGGER_SENT_TITLE[kind]} -- ${description}`);
      } else {
        Alert.alert(TRIGGER_FAILED_TITLE[kind], describeCommandFailure(res, t));
      }
    } catch (e) {
      Alert.alert(TRIGGER_FAILED_TITLE[kind], describeCommandException(e, t));
    } finally {
      setTriggerPending(null);
    }
  }

  function handleArm(mode: ArmMode) {
    const action = mode === 'away' ? armAlarm : mode === 'stay' ? dayArmAlarm : nightArmAlarm;
    runAction(() => action(deviceIdRef.current));
  }

  function handleDisarmPress() {
    Alert.alert(t('Disarm the alarm?'), undefined, [
      { text: t('Cancel'), style: 'cancel' },
      { text: t('OK'), onPress: () => runAction(() => disarmAlarm(deviceIdRef.current)) },
    ]);
  }

  function handleStatusPress() {
    if (status?.armMode || status?.bell) {
      handleDisarmPress();
    } else if (status?.isReady) {
      setArmExpanded((v) => !v);
    } else if (status?.isNotReady) {
      navigation.navigate('Exclusions');
    }
  }

  return (
    <>
    <GradientBackground style={{ flex: 1 }}>
    <ScrollView contentContainerStyle={styles.container}>
      <TopBar
        description={description}
        onMenu={() => setMenuOpen(true)}
        onSwitchPanel={() => navigation.navigate('PanelSelector')}
        onRefresh={fetchStatus}
        refreshing={loading}
      />

      {/* Live reachability wins; `isOnline` (the login-time database flag) is only a placeholder
          for the moment before the first status poll comes back. */}
      {(() => {
        const live = reachabilityOf('ip', deviceId);
        const connected = live === 'unknown' ? !!isOnline : live === 'online';
        return (
          <View style={{ flexDirection: 'row', alignItems: 'center', marginBottom: spacing.md }}>
            <StatusDot color={connected ? colors.okInk : colors.onDarkDim} size={7} />
            <Text style={[typography.bodyDim, { color: colors.onDarkDim, marginLeft: spacing.xs }]}>
              {connected ? t('Connected') : t('Not connected')}
            </Text>
          </View>
        );
      })()}

      {error && <Banner kind="error">{error}</Banner>}

      {!loading && status && (status.memo || status.bypass || status.fail) && (
        <View style={styles.badgeRow}>
          {status.memo && <InfoBadge icon="💾" label={t('Memory')} tone="neutral" onPress={() => navigation.navigate('Memory')} />}
          {status.bypass && (
            <InfoBadge icon="🚩" label={t('Zones excluded')} tone="warning" onPress={() => navigation.navigate('Exclusions')} />
          )}
          {status.fail && <InfoBadge icon="⚠️" label={t('Fault')} tone="danger" onPress={() => navigation.navigate('Failures')} />}
        </View>
      )}

      <View style={{ marginBottom: spacing.lg }}>
        {loading ? (
          <ActivityIndicator size="large" color={colors.accent} />
        ) : (
          <StatusCard status={status} onPress={handleStatusPress} disabled={actionPending} />
        )}
      </View>

      {armExpanded && (
        <View style={[styles.armOptions, { backgroundColor: colors.panel, borderColor: colors.line, borderRadius: radius.md }]}>
          <Text style={[typography.label, { color: colors.inkDim, marginBottom: spacing.sm }]}>{t('Arm mode')}</Text>
          <PrimaryButton title={t('Away')} onPress={() => handleArm('away')} loading={actionPending} />
          <View style={{ height: spacing.sm }} />
          <PrimaryButton title={t('Night')} onPress={() => handleArm('night')} loading={actionPending} />
          <View style={{ height: spacing.sm }} />
          <PrimaryButton title={t('Day')} onPress={() => handleArm('stay')} loading={actionPending} />
        </View>
      )}

      {!loading && status?.isEmpty && (
        <Text style={[typography.bodyDim, { color: colors.onDarkDim, textAlign: 'center', marginBottom: spacing.lg }]}>
          {t("The panel isn't responding. Pull to refresh or tap below to try again.")}
        </Text>
      )}
      {!loading && (status?.isEmpty || status?.isError) && (
        <PrimaryButton title={t('Refresh')} onPress={fetchStatus} loading={actionPending} />
      )}

      <View style={[styles.actionRow, { marginTop: spacing.md }]}>
        <ActionCard
          iconSource={PANIC_ICON}
          label={t('Panic')}
          onPress={() => confirmTrigger('panic', () => panic(deviceIdRef.current))}
          loading={triggerPending === 'panic'}
          disabled={triggerPending !== null || actionPending}
          style={{ flex: 1, marginRight: spacing.sm }}
        />
        <ActionCard
          iconSource={DURESS_ICON}
          label={t('Duress')}
          onPress={() => confirmTrigger('duress', () => duress(deviceIdRef.current))}
          loading={triggerPending === 'duress'}
          disabled={triggerPending !== null || actionPending}
          style={{ flex: 1 }}
        />
      </View>
      <ActionCard
        icon="📢"
        label={t('Emergency')}
        onPress={() => confirmTrigger('emergency', () => emergency(deviceIdRef.current))}
        loading={triggerPending === 'emergency'}
        disabled={triggerPending !== null || actionPending}
        tone="danger"
        style={{ marginTop: spacing.sm }}
      />
    </ScrollView>
    </GradientBackground>
    <Snackbar message={actionToast} onDismiss={() => setActionToast(null)} />
    <LoadingOverlay visible={actionPending} label={t('Please wait')} />
    <MainMenu visible={menuOpen} onClose={() => setMenuOpen(false)} />
    </>
  );
}

// New APP Videos/'s dashboard shows the panel's ready/armed state as a full-width rounded card
// (shield icon + label), not a circular readout -- and Pánico/Asalto Silencioso/Emergencia as
// icon cards (two side by side, one wide below), not three stacked text buttons. Restructured to
// match both; the underlying status-parsing/action logic is unchanged.
//
// The siren visual itself (icon animation + card color) is ported pixel/timing-exact from the
// previous app's home.page.ts/.scss per the client's explicit "Ver APP en uso" request: bellImage
// cycled through 4 PNG frames (notifications_active0-3.svg, extracted and carried over as-is)
// every 250ms via assingBlink()'s setInterval, and .status-icon-bell ran a "mymove" CSS keyframe
// animation (0%/100% = $danger #F37575, 50% = #fff191) over 0.8s, infinite -- reproduced here with
// Animated.loop since RN has no CSS-keyframe equivalent. The red<->pale-yellow interpolation
// naturally passes through orange mid-tones, matching the client's "amarillo, naranja y rojo".
const SIREN_WAVE_INTERVAL_MS = 250;
const SIREN_PULSE_HALF_DURATION_MS = 400;
const SIREN_PULSE_COLOR_FROM = '#F37575';
const SIREN_PULSE_COLOR_TO = '#fff191';
const SIREN_WAVE_FRAMES = [
  require('../../assets/images/siren-wave-0.png'),
  require('../../assets/images/siren-wave-1.png'),
  require('../../assets/images/siren-wave-2.png'),
  require('../../assets/images/siren-wave-3.png'),
];

const AnimatedPressable = Animated.createAnimatedComponent(Pressable);

function StatusCard({
  status,
  onPress,
  disabled,
}: {
  status: ParsedStatus | null;
  onPress: () => void;
  disabled: boolean;
}) {
  const { colors, typography, spacing, radius } = useTheme();
  const { t } = useLocale();
  const [waveFrame, setWaveFrame] = useState(0);
  const pulseAnim = useRef(new Animated.Value(0)).current;

  useEffect(() => {
    if (!status?.bell) return;
    const interval = setInterval(() => setWaveFrame((f) => (f + 1) % SIREN_WAVE_FRAMES.length), SIREN_WAVE_INTERVAL_MS);
    return () => clearInterval(interval);
  }, [status?.bell]);

  useEffect(() => {
    if (!status?.bell) {
      pulseAnim.setValue(0);
      return;
    }
    const loop = Animated.loop(
      Animated.sequence([
        Animated.timing(pulseAnim, { toValue: 1, duration: SIREN_PULSE_HALF_DURATION_MS, useNativeDriver: false }),
        Animated.timing(pulseAnim, { toValue: 0, duration: SIREN_PULSE_HALF_DURATION_MS, useNativeDriver: false }),
      ]),
    );
    loop.start();
    return () => loop.stop();
  }, [status?.bell, pulseAnim]);

  if (!status) return null;

  const { topCaption, label, icon, color, bg } = describeStatus(status, colors, t);
  const pulseBg = pulseAnim.interpolate({ inputRange: [0, 1], outputRange: [SIREN_PULSE_COLOR_FROM, SIREN_PULSE_COLOR_TO] });

  return (
    <AnimatedPressable
      onPress={onPress}
      disabled={disabled}
      style={[
        styles.statusCard,
        {
          backgroundColor: status.bell ? pulseBg : bg,
          borderRadius: radius.lg,
          opacity: disabled ? 0.6 : 1,
          shadowColor: color,
        },
      ]}
    >
      {topCaption && (
        <Text style={[typography.readoutSm, { color, marginBottom: spacing.xs, opacity: 0.85 }]}>{topCaption}</Text>
      )}
      {status.bell ? (
        <Image source={SIREN_WAVE_FRAMES[waveFrame]} style={styles.sirenWaveIcon} resizeMode="contain" />
      ) : (
        <Text style={styles.statusIcon}>{icon}</Text>
      )}
      <Text style={[typography.readout, { color, textAlign: 'center' }]}>{label}</Text>
    </AnimatedPressable>
  );
}

// New APP Videos/'s dashboard puts a small "Armada" caption above the icon for every armed state,
// and the icon/main label are mode-specific (sun+"Modo Día", house+"Modo Ausente", moon+"Modo
// Noche") -- not a generic shield+"Armed" for away mode the way this used to read, with "Armed"
// and the mode crammed onto two separate lines below the icon instead.
function describeStatus(
  status: ParsedStatus,
  colors: ReturnType<typeof useTheme>['colors'],
  t: ReturnType<typeof useLocale>['t'],
) {
  if (status.bell) return { label: t('Siren active'), icon: '🚨', color: colors.accentInk, bg: colors.danger };
  if (status.armMode === 'stay')
    return { topCaption: t('Armed'), label: t('Day mode'), icon: '☀️', color: colors.accentInk, bg: colors.danger };
  if (status.armMode === 'night')
    return { topCaption: t('Armed'), label: t('Night mode'), icon: '🌙', color: colors.accentInk, bg: colors.danger };
  if (status.armMode === 'away')
    return { topCaption: t('Armed'), label: t('Away mode'), icon: '🏠', color: colors.accentInk, bg: colors.danger };
  if (status.isReady) return { label: t('Ready to arm'), icon: '🛡️', color: colors.accentInk, bg: colors.accent };
  if (status.isNotReady) return { label: t('Not ready (zones open)'), icon: '⚠️', color: colors.ink, bg: colors.panel };
  if (status.isError) return { label: t('Command error'), icon: '❗', color: colors.accentInk, bg: colors.danger };
  return { label: t('No response'), icon: '❓', color: colors.ink, bg: colors.panel };
}

// Two side by side (Pánico/Asalto Silencioso) or one full width (Emergencia) -- matches New APP
// Videos/'s dashboard icon cards. tone='danger' is a light-red fill for the wide Emergency card;
// the two small ones use the plain white panel surface, same as the reference.
function ActionCard({
  icon,
  iconSource,
  label,
  onPress,
  loading,
  disabled,
  tone = 'default',
  style,
}: {
  icon?: string;
  iconSource?: number;
  label: string;
  onPress: () => void;
  loading?: boolean;
  disabled?: boolean;
  tone?: 'default' | 'danger';
  style?: object;
}) {
  const { colors, typography, radius } = useTheme();
  const isDanger = tone === 'danger';
  return (
    <Pressable
      onPress={onPress}
      disabled={disabled}
      style={[
        styles.actionCard,
        {
          backgroundColor: isDanger ? colors.danger : colors.panel,
          borderColor: isDanger ? colors.danger : colors.line,
          borderRadius: radius.md,
          opacity: disabled && !loading ? 0.6 : 1,
        },
        style,
      ]}
    >
      {loading ? (
        <ActivityIndicator color={isDanger ? colors.accentInk : colors.accent} />
      ) : (
        <>
          {iconSource ? (
            <Image source={iconSource} style={styles.actionIconImage} resizeMode="contain" />
          ) : (
            <Text style={styles.actionIcon}>{icon}</Text>
          )}
          <Text style={[typography.body, { color: isDanger ? colors.accentInk : colors.ink, fontWeight: '700' }]}>
            {label}
          </Text>
        </>
      )}
    </Pressable>
  );
}

// New APP Videos/'s "Exclusiones" badge is an icon-on-top-of-label mini card -- the same visual
// language as the Pánico/Asalto Silencioso/Emergencia ActionCards below -- not the small dot+text
// pill this used to be.
function InfoBadge({
  icon,
  label,
  tone,
  onPress,
}: {
  icon: string;
  label: string;
  tone: 'neutral' | 'warning' | 'danger';
  onPress?: () => void;
}) {
  const { colors, typography, radius } = useTheme();
  const bg = tone === 'danger' ? colors.dangerBg : tone === 'warning' ? colors.warningBg : colors.panel;
  const badge = (
    <View style={[styles.badge, { backgroundColor: bg, borderColor: colors.line, borderRadius: radius.md }]}>
      <Text style={styles.badgeIcon}>{icon}</Text>
      <Text style={[typography.body, { color: colors.ink, fontWeight: '700' }]}>{label}</Text>
    </View>
  );
  return onPress ? <Pressable style={{ flex: 1 }} onPress={onPress}>{badge}</Pressable> : badge;
}

function SmsHome({ navigation }: { navigation: HomeNavigation }) {
  const { colors, typography, spacing } = useTheme();
  const { t } = useLocale();
  const { session } = useAuth();
  const { selected } = usePanels();
  const [sending, setSending] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [menuOpen, setMenuOpen] = useState(false);

  const smsDevice =
    selected?.kind === 'sms' ? session?.account.SmsDevices.find((d) => d.DeviceId === selected.deviceId) : undefined;

  async function send(build: (simPin: string) => string) {
    if (!smsDevice) return;
    setError(null);
    setSending(true);
    try {
      const opened = await openSmsComposer(smsDevice.PhoneNumber, build(smsDevice.SimPin));
      if (!opened) setError(t('Could not open your messaging app.'));
    } catch {
      setError(t('Could not open your messaging app.'));
    } finally {
      setSending(false);
    }
  }

  return (
    <>
    <GradientBackground style={{ flex: 1 }}>
    <ScrollView contentContainerStyle={styles.container}>
      <TopBar
        description={smsDevice?.Description ?? t('SMS panel')}
        onMenu={() => setMenuOpen(true)}
        onSwitchPanel={() => navigation.navigate('PanelSelector')}
      />
      <Text style={[typography.bodyDim, { color: colors.onDarkDim, marginBottom: spacing.lg }]}>
        {t(
          "SMS panels aren't monitored live -- each action sends a text message from your own phone, one confirmation tap away.",
        )}
      </Text>

      {error && <Banner kind="error">{error}</Banner>}

      <PrimaryButton title={t('Check status')} onPress={() => send(SmsCommands.status)} loading={sending} />
      <View style={{ height: spacing.md }} />
      <PrimaryButton title={t('Arm away')} onPress={() => send(SmsCommands.armAway)} loading={sending} />
      <View style={{ height: spacing.md }} />
      <PrimaryButton title={t('Arm day')} onPress={() => send(SmsCommands.armDay)} loading={sending} />
      <View style={{ height: spacing.md }} />
      <PrimaryButton title={t('Arm night')} onPress={() => send(SmsCommands.armNight)} loading={sending} />
      <View style={{ height: spacing.md }} />
      <PrimaryButton title={t('Disarm')} onPress={() => send(SmsCommands.disarm)} loading={sending} tone="danger" />

      {/* New APP Videos/'s SMS dashboard shows Pánico/Asalto Silencioso/Emergencia as the same
          icon cards as the IP dashboard (see IpHome's ActionCard usage above), not stacked text
          buttons -- matched here; the arm/disarm/status actions above have no equivalent shown
          in the reference, so they keep the existing button list rather than guessing a layout. */}
      <View style={[styles.actionRow, { marginTop: spacing.md }]}>
        <ActionCard iconSource={PANIC_ICON} label={t('Panic')} onPress={() => send(SmsCommands.panic)} loading={sending} style={{ flex: 1, marginRight: spacing.sm }} />
        <ActionCard iconSource={DURESS_ICON} label={t('Duress')} onPress={() => send(SmsCommands.duress)} loading={sending} style={{ flex: 1 }} />
      </View>
      <ActionCard
        icon="📢"
        label={t('Emergency')}
        onPress={() => send(SmsCommands.emergency)}
        loading={sending}
        tone="danger"
        style={{ marginTop: spacing.sm }}
      />
    </ScrollView>
    </GradientBackground>
    <MainMenu visible={menuOpen} onClose={() => setMenuOpen(false)} />
    </>
  );
}

// New APP Videos/'s dashboard puts the drawer hamburger, a refresh icon, and the panel-name pill
// (bookmark icon + name) inline on the gradient body itself -- no teal header bar on Home at all
// (see MainStack.tsx's headerShown: false for this route). SMS panels have no live status to
// refresh, so onRefresh is optional and simply omitted there.
function TopBar({
  description,
  onMenu,
  onSwitchPanel,
  onRefresh,
  refreshing,
}: {
  description: string;
  onMenu: () => void;
  onSwitchPanel: () => void;
  onRefresh?: () => void;
  refreshing?: boolean;
}) {
  const { colors, typography, spacing, radius } = useTheme();
  return (
    <View style={[styles.topBar, { marginBottom: spacing.lg }]}>
      <MenuButton onPress={onMenu} />
      {onRefresh && (
        <Pressable
          onPress={onRefresh}
          disabled={refreshing}
          style={[styles.refreshCircle, { backgroundColor: colors.panel, opacity: refreshing ? 0.5 : 1 }]}
        >
          <Text style={{ fontSize: 16, color: colors.accent }}>{'⟳'}</Text>
        </Pressable>
      )}
      <Pressable
        onPress={onSwitchPanel}
        style={[styles.panelPill, { backgroundColor: colors.panel, borderRadius: radius.pill }]}
      >
        <Text style={{ fontSize: 15, marginRight: 8 }}>{'\u{1F516}'}</Text>
        <Text style={[typography.body, { color: colors.ink, fontWeight: '700' }]} numberOfLines={1}>
          {description}
        </Text>
      </Pressable>
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flexGrow: 1,
    padding: 24,
  },
  centered: {
    flex: 1,
    alignItems: 'center',
    justifyContent: 'center',
    padding: 24,
  },
  emptyCard: {
    width: '100%',
    maxWidth: 320,
    alignItems: 'stretch',
  },
  emptyLogo: {
    width: 72,
    height: 72,
    alignSelf: 'center',
    marginBottom: 12,
  },
  topBar: {
    flexDirection: 'row',
    alignItems: 'center',
  },
  refreshCircle: {
    width: 36,
    height: 36,
    borderRadius: 18,
    alignItems: 'center',
    justifyContent: 'center',
    marginLeft: 12,
  },
  panelPill: {
    flex: 1,
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    paddingVertical: 8,
    paddingHorizontal: 14,
    marginLeft: 12,
  },
  statusCard: {
    width: '100%',
    alignItems: 'center',
    justifyContent: 'center',
    paddingVertical: 32,
    shadowOffset: { width: 0, height: 8 },
    shadowOpacity: 0.2,
    shadowRadius: 16,
    elevation: 3,
  },
  statusIcon: {
    fontSize: 48,
    marginBottom: 8,
  },
  // 356x200 source aspect ratio (notifications_active*.svg), scaled down to roughly match
  // statusIcon's footprint.
  sirenWaveIcon: {
    width: 100,
    height: 56,
    marginBottom: 8,
  },
  badgeRow: {
    flexDirection: 'row',
    gap: 8,
    marginBottom: 16,
  },
  // Sized to match the previous app's Memoria/Exclusiones/Fallas buttons (client's "Pruebas
  // 1-10-26" feedback) -- same footprint as the Pánico/Duress ActionCards below, not the smaller
  // pill these used to be.
  badge: {
    flex: 1,
    alignItems: 'center',
    justifyContent: 'center',
    borderWidth: 1,
    paddingVertical: 20,
  },
  badgeIcon: {
    fontSize: 28,
    marginBottom: 6,
  },
  armOptions: {
    borderWidth: 1,
    padding: 16,
    marginBottom: 16,
  },
  actionRow: {
    flexDirection: 'row',
  },
  actionCard: {
    borderWidth: 1,
    alignItems: 'center',
    justifyContent: 'center',
    paddingVertical: 20,
  },
  actionIcon: {
    fontSize: 28,
    marginBottom: 6,
  },
  // Client's "Pruebas 1-10-26" feedback: enlarge the Pánico/Asalto Silencioso icons to match the
  // previous app's reference size -- these read as small relative to the card at 40x40.
  actionIconImage: {
    width: 72,
    height: 72,
    marginBottom: 6,
  },
});

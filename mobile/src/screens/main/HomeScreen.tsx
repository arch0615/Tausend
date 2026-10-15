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
import { describeCommandException, describeCommandFailure, panelAnswered } from '../../panels/commandFeedback';
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
// Memoria / Exclusiones / Fallas, lifted from the previous app's own assets (memory.svg,
// browser_not_supported.svg, error_outline.svg). The client has asked twice for these three to
// match the app they actually use -- "los iconos de fallas, exclusiones y memorias deben ser
// iguales (ya fue solicitado en otras ocasiones)". An earlier reading took that to mean
// "consistent with each other", which is why they became tinted glyphs; they meant the old app's
// icons. EMERGENCY_ICON is that app's warning.svg, replacing the one emoji still left among the
// three action cards.
const MEMORY_ICON = require('../../assets/images/icon-memory.png');
const EXCLUSIONS_ICON = require('../../assets/images/icon-exclusions.png');
const FAILURES_ICON = require('../../assets/images/icon-failures.png');
const EMERGENCY_ICON = require('../../assets/images/icon-emergency-megaphone.png');
// The dashboard's own state icons, from the same previous-app asset set: lock_open (ready),
// house (away), wb_sunny (day), nights_stay (night) and central (the panel pill). The siren
// frames beside them were already imported from that app in an earlier round and are pixel
// identical to its notifications_active0-3, so these finish a set that was left half done in
// emoji. The three states the old dashboard has no equivalent for (not ready, command error, no
// response) keep their glyph.
const READY_ICON = require('../../assets/images/icon-ready-shield.png');
const AWAY_ICON = require('../../assets/images/icon-mode-away.png');
const DAY_ICON = require('../../assets/images/icon-mode-day.png');
const NIGHT_ICON = require('../../assets/images/icon-mode-night.png');
const PANEL_ICON = require('../../assets/images/icon-panel.png');
// The previous app has no card for "no response" or "command error" -- it keeps spinning -- and
// it draws any disarmed state with lock_open. So rather than leave these three on emoji, which
// is what the client objected to, not-ready reuses that same shield and the two unreadable
// states use its own "consultar estado" magnifier.
const STATE_UNKNOWN_ICON = require('../../assets/images/icon-state-unknown.png');

// home.page.scss's .fail-button and its ion-label, verbatim: the size the client asked for in
// both rounds ("deben ser como los de la APP en uso (la anterior)").
// The rest of home.page.scss's main-screen rules, same source as INFO_BUTTON below. Kept local
// to this screen rather than pushed into the theme: accent and danger style every other screen's
// buttons and links, so changing those tokens would restyle the whole app, and the client asked
// for the main screen.
const MAIN = {
  statusHeight: 150,          // .status-icon
  statusRadius: 10,
  // Its img is max-height 120, but that is a CSS max in a flex column that shrinks. A fixed
  // height here does not shrink, and 120 + the 18pt caption + the 18pt label overflows the 150
  // box, so this is the largest value that still fits the armed state without clipping.
  statusIconMaxHeight: 110,
  statusGreen: '#69C1AB',     // .status-green  ($green)
  statusRed: '#F37575',       // .status-red    ($danger)
  statusGray: '#848484',      // .status-gray
  actionHeight: 91,           // .btn-bottom
  actionRadius: 4,
  actionBg: '#fafafa',
  actionIcon: 60,             // .icons-bottom
  emergencyHeight: 110,       // .btn-emergency
  emergencyIcon: 80,
  labelColor: '#0D455C',      // $primary
  labelSize: 12,              // .column ion-text
  pillHeight: 50,             // .btn-selector
  pillRadius: 6,
  pillLabelSize: 16,
  // .button-option: the three mode buttons that open from the status card
  optionHeight: 140,
  optionGap: 10,
  optionIcon: 70,
  // .flex-colum / .flex-row gap between the bottom buttons
  bottomGap: 16,
  shadow: { shadowColor: '#000', shadowOpacity: 0.3, shadowRadius: 4, shadowOffset: { width: 0, height: 3 }, elevation: 3 },
};

const INFO_BUTTON = {
  width: 77,
  height: 50,
  background: '#fafafa',
  border: '#f2f3f3',
  radius: 4,
  labelColor: '#0D455C',
  labelSize: 10,
  iconSize: 30,
};

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
  const { colors, typography, spacing } = useTheme();
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
    // Did the PANEL itself answer, as opposed to the request merely completing? The two are not
    // the same: the backend happily returns 200 with CENTRAL_UNRESPONSIVE or an empty Text when
    // the panel is unreachable. Tracking only `lastResponse` treated that as success and reported
    // the panel online, which is why the connection indicator kept saying "Conectada" after a
    // panel lost its link to the server (client report, 14 Oct, video 5).
    let answered = false;
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
        if (panelAnswered(res)) {
          answered = true;
          break;
        }
      } catch (e) {
        lastResponse = null;
        lastException = e;
      }
      if (attempt < STATUS_MAX_ATTEMPTS - 1) await delay(STATUS_RETRY_DELAY_MS);
    }
    setLoading(false);
    if (!lastResponse || !answered) {
      setError(lastException ? describeCommandException(lastException, t) : t("The panel isn't responding -- it may be offline."));
      setStatus(null);
      // Every retry above is exhausted at this point, so this is a real "cannot reach it", not a
      // single dropped packet. Covers both a transport failure (no response at all) and a
      // response that came back saying the panel never replied.
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
          {status.memo && (
            <InfoBadge iconSource={MEMORY_ICON} label={t('Memory')} onPress={() => navigation.navigate('Memory')} />
          )}
          {status.bypass && (
            <InfoBadge iconSource={EXCLUSIONS_ICON} label={t('Zones excluded')} onPress={() => navigation.navigate('Exclusions')} />
          )}
          {status.fail && (
            <InfoBadge iconSource={FAILURES_ICON} label={t('Fault')} onPress={() => navigation.navigate('Failures')} />
          )}
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
        // The previous app opens three green mode buttons side by side under the status card
        // (.button-option: 140 tall, ~30% wide, gap 10), each an icon over its own caption --
        // not a white card with stacked text buttons the way this read before.
        <View style={styles.armOptions}>
          <ArmOption iconSource={AWAY_ICON} label={t('Arm away mode')} onPress={() => handleArm('away')} disabled={actionPending} />
          <ArmOption iconSource={NIGHT_ICON} label={t('Arm night mode')} onPress={() => handleArm('night')} disabled={actionPending} />
          <ArmOption iconSource={DAY_ICON} label={t('Arm day mode')} onPress={() => handleArm('stay')} disabled={actionPending} />
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
          style={{ flex: 1, marginRight: MAIN.bottomGap }}
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
        iconSource={EMERGENCY_ICON}
        label={t('Emergency')}
        onPress={() => confirmTrigger('emergency', () => emergency(deviceIdRef.current))}
        loading={triggerPending === 'emergency'}
        disabled={triggerPending !== null || actionPending}
        tone="danger"
        style={{ marginTop: MAIN.bottomGap }}
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
  const { colors } = useTheme();
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

  const { topCaption, label, icon, iconSource, color, bg } = describeStatus(status, colors, t) as ReturnType<typeof describeStatus> & { icon?: string; iconSource?: number };
  const pulseBg = pulseAnim.interpolate({ inputRange: [0, 1], outputRange: [SIREN_PULSE_COLOR_FROM, SIREN_PULSE_COLOR_TO] });

  return (
    <AnimatedPressable
      onPress={onPress}
      disabled={disabled}
      style={[
        styles.statusCard,
        {
          // .status-icon: 150 tall, radius 10, its own drop shadow.
          height: MAIN.statusHeight,
          backgroundColor: status.bell ? pulseBg : bg,
          borderRadius: MAIN.statusRadius,
          opacity: disabled ? 0.6 : 1,
        },
      ]}
    >
      {topCaption && <Text style={[styles.statusText, { color }]}>{topCaption}</Text>}
      {status.bell ? (
        <Image source={SIREN_WAVE_FRAMES[waveFrame]} style={styles.sirenWaveIcon} resizeMode="contain" />
      ) : iconSource ? (
        <Image source={iconSource} style={styles.statusIconImage} resizeMode="contain" />
      ) : (
        <Text style={styles.statusIcon}>{icon}</Text>
      )}
      <Text style={[styles.statusText, { color, textAlign: 'center' }]}>{label}</Text>
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
  if (status.bell) return { label: t('Siren active'), color: colors.accentInk, bg: MAIN.statusRed };
  if (status.armMode === 'stay')
    return { topCaption: t('Armed'), label: t('Day mode'), iconSource: DAY_ICON, color: colors.accentInk, bg: MAIN.statusRed };
  if (status.armMode === 'night')
    return { topCaption: t('Armed'), label: t('Night mode'), iconSource: NIGHT_ICON, color: colors.accentInk, bg: MAIN.statusRed };
  if (status.armMode === 'away')
    return { topCaption: t('Armed'), label: t('Away mode'), iconSource: AWAY_ICON, color: colors.accentInk, bg: MAIN.statusRed };
  if (status.isReady)
    return { label: t('Ready to arm'), iconSource: READY_ICON, color: colors.accentInk, bg: MAIN.statusGreen };
  if (status.isNotReady)
    return { label: t('Not ready (zones open)'), iconSource: READY_ICON, color: colors.accentInk, bg: MAIN.statusGray };
  if (status.isError)
    return { label: t('Command error'), iconSource: STATE_UNKNOWN_ICON, color: colors.accentInk, bg: MAIN.statusRed };
  return { label: t('No response'), iconSource: STATE_UNKNOWN_ICON, color: colors.accentInk, bg: MAIN.statusGray };
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
  const { colors } = useTheme();
  const isDanger = tone === 'danger';
  return (
    <Pressable
      onPress={onPress}
      disabled={disabled}
      style={[
        styles.actionCard,
        {
          // .btn-bottom is 91 tall on #fafafa; .btn-emergency is 110 on $danger, both radius 4
          // with no border, only the shadow.
          height: isDanger ? MAIN.emergencyHeight : MAIN.actionHeight,
          backgroundColor: isDanger ? MAIN.statusRed : MAIN.actionBg,
          borderRadius: MAIN.actionRadius,
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
            <Image
              source={iconSource}
              style={{ width: isDanger ? MAIN.emergencyIcon : MAIN.actionIcon, height: isDanger ? MAIN.emergencyIcon : MAIN.actionIcon }}
              resizeMode="contain"
            />
          ) : (
            <Text style={styles.actionIcon}>{icon}</Text>
          )}
          <Text style={{ color: isDanger ? '#ffffff' : MAIN.labelColor, fontSize: MAIN.labelSize }}>{label}</Text>
        </>
      )}
    </Pressable>
  );
}

// Memoria / Exclusiones / Fallas, matching the previous app's .fail-button: a fixed 77x50
// near-white tile with that app's own icon above a 10px label, laid out as a centred row rather
// than three cards stretched across the width. See INFO_BUTTON above.
function InfoBadge({
  iconSource,
  label,
  onPress,
}: {
  iconSource: number;
  label: string;
  onPress?: () => void;
}) {
  const badge = (
    <View style={styles.badge}>
      <Image source={iconSource} style={styles.badgeIcon} resizeMode="contain" />
      <Text style={styles.badgeLabel} numberOfLines={1}>
        {label}
      </Text>
    </View>
  );
  // hitSlop keeps the tap target comfortable even though the tile itself is deliberately small.
  return onPress ? (
    <Pressable onPress={onPress} hitSlop={8}>
      {badge}
    </Pressable>
  ) : (
    badge
  );
}

// One of the three mode buttons above: .button-option, a green tile with the mode's own icon
// over a 12px caption.
function ArmOption({
  iconSource,
  label,
  onPress,
  disabled,
}: {
  iconSource: number;
  label: string;
  onPress: () => void;
  disabled?: boolean;
}) {
  return (
    <Pressable onPress={onPress} disabled={disabled} style={[styles.armOption, { opacity: disabled ? 0.6 : 1 }]}>
      <Image source={iconSource} style={styles.armOptionIcon} resizeMode="contain" />
      <Text style={styles.armOptionLabel} numberOfLines={2}>
        {label}
      </Text>
    </Pressable>
  );
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
        <ActionCard iconSource={PANIC_ICON} label={t('Panic')} onPress={() => send(SmsCommands.panic)} loading={sending} style={{ flex: 1, marginRight: MAIN.bottomGap }} />
        <ActionCard iconSource={DURESS_ICON} label={t('Duress')} onPress={() => send(SmsCommands.duress)} loading={sending} style={{ flex: 1 }} />
      </View>
      <ActionCard
        iconSource={EMERGENCY_ICON}
        label={t('Emergency')}
        onPress={() => send(SmsCommands.emergency)}
        loading={sending}
        tone="danger"
        style={{ marginTop: MAIN.bottomGap }}
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
  const { colors, spacing } = useTheme();
  return (
    <View style={[styles.topBar, { marginBottom: spacing.lg }]}>
      <MenuButton onPress={onMenu} />
      {onRefresh && (
        <Pressable
          onPress={onRefresh}
          disabled={refreshing}
          hitSlop={10}
          style={[styles.refreshCircle, { backgroundColor: '#ffffff', opacity: refreshing ? 0.5 : 1 }]}
        >
          <Text style={{ fontSize: 16, color: colors.accent }}>{'⟳'}</Text>
        </Pressable>
      )}
      <Pressable
        onPress={onSwitchPanel}
        style={[styles.panelPill, { backgroundColor: '#ffffff', borderRadius: MAIN.pillRadius, borderWidth: 1, borderColor: INFO_BUTTON.border }]}
      >
        <Image source={PANEL_ICON} style={styles.panelPillIcon} resizeMode="contain" />
        {/* .btn-selector p: 16px, $primary, weight 400 */}
        <Text style={{ color: MAIN.labelColor, fontSize: MAIN.pillLabelSize }} numberOfLines={1}>
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
  // .btn-refresh: 30x30 circle, white with a 1px #f2f3f3 edge and a soft drop shadow.
  refreshCircle: {
    width: 30,
    height: 30,
    borderRadius: 15,
    borderWidth: 1,
    borderColor: '#f2f3f3',
    alignItems: 'center',
    justifyContent: 'center',
    marginLeft: 12,
    shadowColor: '#000',
    shadowOpacity: 0.25,
    shadowRadius: 4,
    shadowOffset: { width: 0, height: 2 },
    elevation: 2,
  },
  // .btn-selector: 50 tall, radius 6, 10px/16px padding, its own shadow.
  panelPill: {
    flex: 1,
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    height: MAIN.pillHeight,
    paddingVertical: 10,
    paddingHorizontal: 16,
    marginLeft: 12,
    ...MAIN.shadow,
  },
  statusCard: {
    width: '100%',
    alignItems: 'center',
    justifyContent: 'center',
    // .status-icon's own shadow: 0px 4px 4px rgba(0,0,0,0.25)
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 4 },
    shadowOpacity: 0.25,
    shadowRadius: 4,
    elevation: 4,
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
  // The previous app centres this row (ion-justify-content-center) with the tiles at their own
  // fixed width, rather than stretching them edge to edge.
  badgeRow: {
    flexDirection: 'row',
    justifyContent: 'center',
    gap: 12,
    marginBottom: 16,
  },
  badge: {
    width: INFO_BUTTON.width,
    height: INFO_BUTTON.height,
    alignItems: 'center',
    justifyContent: 'center',
    backgroundColor: INFO_BUTTON.background,
    borderWidth: 1,
    borderColor: INFO_BUTTON.border,
    borderRadius: INFO_BUTTON.radius,
    // box-shadow: 0px 3px 4px rgba(0,0,0,0.3)
    shadowColor: '#000',
    shadowOpacity: 0.3,
    shadowRadius: 4,
    shadowOffset: { width: 0, height: 3 },
    elevation: 3,
  },
  badgeIcon: {
    width: INFO_BUTTON.iconSize,
    height: INFO_BUTTON.iconSize,
  },
  badgeLabel: {
    color: INFO_BUTTON.labelColor,
    fontSize: INFO_BUTTON.labelSize,
    marginTop: 2,
  },
  armOptions: {
    flexDirection: 'row',
    gap: MAIN.optionGap,
    marginBottom: 16,
  },
  armOption: {
    flex: 1,
    height: MAIN.optionHeight,
    alignItems: 'center',
    justifyContent: 'center',
    paddingHorizontal: 6,
    backgroundColor: MAIN.statusGreen,
    borderRadius: MAIN.actionRadius,
    shadowColor: '#000',
    shadowOpacity: 0.25,
    shadowRadius: 4,
    shadowOffset: { width: 0, height: 4 },
    elevation: 4,
  },
  armOptionIcon: {
    width: MAIN.optionIcon,
    height: MAIN.optionIcon,
    marginBottom: 8,
  },
  armOptionLabel: {
    color: '#ffffff',
    fontSize: MAIN.labelSize,
    lineHeight: 18,
    height: 36,
    textAlign: 'center',
    textAlignVertical: 'center',
  },
  actionRow: {
    flexDirection: 'row',
  },
  actionCard: {
    alignItems: 'center',
    justifyContent: 'center',
    padding: 10,
    ...MAIN.shadow,
  },
  actionIcon: {
    fontSize: 28,
    marginBottom: 6,
  },
  // Client's "Pruebas 1-10-26" feedback: enlarge the Pánico/Asalto Silencioso icons to match the
  // previous app's reference size -- these read as small relative to the card at 40x40.
  // The previous app draws this state icon at max-height 120px inside the status button.
  // .column ion-text: 12px, weight 400, line-height 18. The status card used to set this in
  // 20px/800, which is the single biggest visual difference from the previous app's card.
  statusText: {
    fontSize: MAIN.labelSize,
    fontWeight: '400',
    lineHeight: 18,
  },
  statusIconImage: {
    width: MAIN.statusIconMaxHeight,
    height: MAIN.statusIconMaxHeight,
    marginBottom: 4,
  },
  panelPillIcon: {
    width: 22,
    height: 22,
    marginRight: 10,
  },
  actionIconImage: {
    width: 72,
    height: 72,
    marginBottom: 6,
  },
});

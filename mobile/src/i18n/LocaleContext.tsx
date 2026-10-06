import { createContext, useContext, useEffect, useMemo, useState, type ReactNode } from 'react';
import { NativeModules, Platform } from 'react-native';
import AsyncStorage from '@react-native-async-storage/async-storage';
import { es } from './es';

export type Locale = 'en' | 'es';

const STORAGE_KEY = 'tausend-locale';

// Reads the device's own locale via RN's already-linked native modules -- no new dependency
// (react-native-localize/expo-localization both require native linking we can't verify compiles
// without device/emulator access, see notifications/README.md's Firebase note for the same
// reasoning). Only used as the first-launch default; the user can always override it below.
// Every customer coming from the previous (Spanish-only) app expects Spanish immediately, so an
// undetectable or non-Spanish device locale still falls back to Spanish rather than English --
// English is opt-in only, via the language switcher.
function detectDeviceLocale(): Locale {
  try {
    const raw: string | undefined =
      Platform.OS === 'ios'
        ? NativeModules.SettingsManager?.settings?.AppleLocale ??
          NativeModules.SettingsManager?.settings?.AppleLanguages?.[0]
        : NativeModules.I18nManager?.localeIdentifier;
    return raw?.toLowerCase().startsWith('en') ? 'en' : 'es';
  } catch {
    return 'es';
  }
}

interface LocaleContextValue {
  locale: Locale;
  setLocale: (locale: Locale) => void;
  // Identity-key translation: English text IS the key, e.g. t('Zones') -- no separate en.json to
  // keep in sync, since English is just "no translation found, show the key itself." params does
  // simple {name}-style interpolation for the handful of strings that need it.
  t: (key: string, params?: Record<string, string | number>) => string;
}

const LocaleContext = createContext<LocaleContextValue | null>(null);

function interpolate(text: string, params?: Record<string, string | number>): string {
  if (!params) return text;
  return Object.entries(params).reduce((acc, [k, v]) => acc.replace(`{${k}}`, String(v)), text);
}

export function LocaleProvider({ children }: { children: ReactNode }) {
  // Renders with the 'es' default immediately rather than blocking on AsyncStorage -- matches
  // PanelContext's own hydration approach: a brief flash of the default before the stored/
  // detected locale applies, the same tradeoff already accepted elsewhere in this app.
  const [locale, setLocaleState] = useState<Locale>('es');

  useEffect(() => {
    (async () => {
      try {
        const stored = await AsyncStorage.getItem(STORAGE_KEY);
        if (stored === 'en' || stored === 'es') {
          setLocaleState(stored);
        } else {
          setLocaleState(detectDeviceLocale());
        }
      } catch {
        // Fall back silently -- the 'en' default from useState is already applied.
      }
    })();
  }, []);

  function setLocale(next: Locale) {
    setLocaleState(next);
    AsyncStorage.setItem(STORAGE_KEY, next).catch(() => {});
  }

  const t = useMemo(() => {
    const dict = locale === 'es' ? es : {};
    return (key: string, params?: Record<string, string | number>) => interpolate(dict[key] ?? key, params);
  }, [locale]);

  return <LocaleContext.Provider value={{ locale, setLocale, t }}>{children}</LocaleContext.Provider>;
}

export function useLocale(): LocaleContextValue {
  const ctx = useContext(LocaleContext);
  if (!ctx) throw new Error('useLocale must be used within LocaleProvider');
  return ctx;
}

import { ActivityIndicator, Modal, StyleSheet, Text, View } from 'react-native';
import { useTheme } from '../theme/ThemeProvider';
import { useLocale } from '../i18n/LocaleContext';

// New APP Videos/ shows a centered "Cargando" dialog (Ionic's LoadingController) for every
// in-flight network call, not an inline spinner inside the button that triggered it -- this
// restores that pattern for the screens that need it, rather than each screen inventing its own
// modal.
export function LoadingOverlay({ visible, label }: { visible: boolean; label?: string }) {
  const { colors, typography, spacing, radius } = useTheme();
  const { t } = useLocale();
  return (
    <Modal visible={visible} transparent animationType="fade">
      <View style={styles.backdrop}>
        <View style={[styles.box, { backgroundColor: colors.panel, borderRadius: radius.md }]}>
          <ActivityIndicator color={colors.accent} />
          <Text style={[typography.body, { color: colors.ink, marginLeft: spacing.md }]}>{label ?? t('Loading')}</Text>
        </View>
      </View>
    </Modal>
  );
}

const styles = StyleSheet.create({
  backdrop: {
    flex: 1,
    alignItems: 'center',
    justifyContent: 'center',
    backgroundColor: 'rgba(0,0,0,0.15)',
  },
  box: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingVertical: 16,
    paddingHorizontal: 24,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 4 },
    shadowOpacity: 0.2,
    shadowRadius: 10,
    elevation: 6,
  },
});

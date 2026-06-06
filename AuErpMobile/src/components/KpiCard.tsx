import React from 'react';
import {StyleSheet, Text, View} from 'react-native';
import Icon from 'react-native-vector-icons/MaterialIcons';
import {Colors} from '../theme/colors';

interface Trend {
  value: string;
  positive?: boolean;
}

interface Props {
  label: string;
  value: string;
  sub?: string;
  accent?: string;
  icon?: string;
  fullWidth?: boolean;
  variant?: 'default' | 'compact' | 'hero';
  trend?: Trend;
}

export default function KpiCard({
  label,
  value,
  sub,
  accent = Colors.blue,
  icon,
  fullWidth,
  variant = 'default',
  trend,
}: Props) {
  const tint = accent + '14';
  const isCompact = variant === 'compact';
  const isHero = variant === 'hero';

  return (
    <View style={[styles.card, fullWidth && styles.fullWidth, isCompact && styles.cardCompact, isHero && styles.cardHero]}>
      <View style={[styles.accentBar, {backgroundColor: accent}, isCompact && styles.accentBarCompact]} />
      <View style={[styles.body, isCompact && styles.bodyCompact, isHero && styles.bodyHero]}>
        <View style={styles.topRow}>
          <Text style={[styles.label, isCompact && styles.labelCompact]}>{label}</Text>
          {icon ? (
            <View style={[styles.iconBadge, {backgroundColor: tint}, isCompact && styles.iconBadgeCompact]}>
              <Icon name={icon} size={isCompact ? 14 : 16} color={accent} />
            </View>
          ) : null}
        </View>
        <Text
          style={[styles.value, {color: accent}, isCompact && styles.valueCompact, isHero && styles.valueHero]}
          adjustsFontSizeToFit
          numberOfLines={1}
          minimumFontScale={0.65}>
          {value}
        </Text>
        {trend ? (
          <Text style={[styles.trend, trend.positive ? styles.trendUp : styles.trendDown]}>{trend.value}</Text>
        ) : null}
        {sub ? <Text style={[styles.sub, isCompact && styles.subCompact]}>{sub}</Text> : null}
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  card: {
    backgroundColor: Colors.card,
    borderRadius: 12,
    flexDirection: 'row',
    overflow: 'hidden',
    shadowColor: Colors.shadow,
    shadowOpacity: 0.08,
    shadowRadius: 6,
    shadowOffset: {width: 0, height: 3},
    elevation: 3,
    flex: 1,
    minWidth: 140,
  },
  cardCompact: {minWidth: 120},
  cardHero: {borderRadius: 14, elevation: 4},
  fullWidth: {flex: 0, width: '100%'},
  accentBar: {width: 5},
  accentBarCompact: {width: 4},
  body: {flex: 1, padding: 16},
  bodyCompact: {padding: 12},
  bodyHero: {padding: 18},
  topRow: {flexDirection: 'row', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: 6},
  label: {
    fontSize: 11,
    fontWeight: '700',
    color: Colors.subtle,
    letterSpacing: 0.8,
    textTransform: 'uppercase',
    flex: 1,
    paddingRight: 6,
  },
  labelCompact: {fontSize: 10},
  iconBadge: {
    width: 28,
    height: 28,
    borderRadius: 8,
    justifyContent: 'center',
    alignItems: 'center',
  },
  iconBadgeCompact: {width: 24, height: 24, borderRadius: 6},
  value: {fontSize: 26, fontWeight: '800', letterSpacing: -0.5, marginBottom: 2},
  valueCompact: {fontSize: 22},
  valueHero: {fontSize: 30},
  trend: {fontSize: 11, fontWeight: '700', marginBottom: 2},
  trendUp: {color: Colors.green},
  trendDown: {color: Colors.red},
  sub: {fontSize: 12, color: Colors.subtle, fontWeight: '500'},
  subCompact: {fontSize: 11},
});

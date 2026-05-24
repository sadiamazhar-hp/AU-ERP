import React from 'react';
import {StyleSheet, Text, View} from 'react-native';
import Icon from 'react-native-vector-icons/MaterialIcons';
import {Colors} from '../theme/colors';

interface Props {
  label: string;
  value: string;
  sub?: string;
  accent?: string;
  icon?: string;
  fullWidth?: boolean;
}

export default function KpiCard({label, value, sub, accent = Colors.blue, icon, fullWidth}: Props) {
  const tint = accent + '14'; // ~8% opacity hex
  return (
    <View style={[styles.card, fullWidth && styles.fullWidth]}>
      <View style={[styles.accentBar, {backgroundColor: accent}]} />
      <View style={styles.body}>
        <View style={styles.topRow}>
          <Text style={styles.label}>{label}</Text>
          {icon ? (
            <View style={[styles.iconBadge, {backgroundColor: tint}]}>
              <Icon name={icon} size={16} color={accent} />
            </View>
          ) : null}
        </View>
        <Text style={[styles.value, {color: accent}]}>{value}</Text>
        {sub ? <Text style={styles.sub}>{sub}</Text> : null}
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
  fullWidth: {flex: 0, width: '100%'},
  accentBar: {width: 5},
  body: {flex: 1, padding: 16},
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
  iconBadge: {
    width: 28,
    height: 28,
    borderRadius: 8,
    justifyContent: 'center',
    alignItems: 'center',
  },
  value: {fontSize: 26, fontWeight: '800', letterSpacing: -0.5, marginBottom: 2},
  sub: {fontSize: 12, color: Colors.subtle, fontWeight: '500'},
});

import React from 'react';
import {StyleSheet, Text, View} from 'react-native';
import {Colors} from '../theme/colors';

interface LegendItem {
  label: string;
  color: string;
}

interface Props {
  title?: string;
  subtitle?: string;
  legend?: LegendItem[];
  children: React.ReactNode;
  empty?: boolean;
  emptyMessage?: string;
}

export default function ChartCard({
  title,
  subtitle,
  legend,
  children,
  empty,
  emptyMessage = 'No data for selected period',
}: Props) {
  return (
    <View style={styles.card}>
      {(title || subtitle) && (
        <View style={styles.header}>
          {title ? <Text style={styles.title}>{title}</Text> : null}
          {subtitle ? <Text style={styles.subtitle}>{subtitle}</Text> : null}
        </View>
      )}
      {legend && legend.length > 0 && (
        <View style={styles.legendRow}>
          {legend.map(item => (
            <View key={item.label} style={styles.legendItem}>
              <View style={[styles.legendDot, {backgroundColor: item.color}]} />
              <Text style={styles.legendText}>{item.label}</Text>
            </View>
          ))}
        </View>
      )}
      {empty ? (
        <View style={styles.empty}>
          <Text style={styles.emptyText}>{emptyMessage}</Text>
        </View>
      ) : (
        children
      )}
    </View>
  );
}

const styles = StyleSheet.create({
  card: {
    backgroundColor: Colors.card,
    borderRadius: 14,
    padding: 14,
    marginBottom: 12,
    shadowColor: Colors.shadow,
    shadowOpacity: 0.08,
    shadowRadius: 8,
    shadowOffset: {width: 0, height: 3},
    elevation: 3,
    borderWidth: 1,
    borderColor: Colors.border,
  },
  header: {marginBottom: 10},
  title: {fontSize: 14, fontWeight: '700', color: Colors.text},
  subtitle: {fontSize: 12, color: Colors.subtle, marginTop: 2, fontWeight: '500'},
  legendRow: {flexDirection: 'row', flexWrap: 'wrap', gap: 12, marginBottom: 8},
  legendItem: {flexDirection: 'row', alignItems: 'center', gap: 6},
  legendDot: {width: 8, height: 8, borderRadius: 4},
  legendText: {fontSize: 11, color: Colors.textSecondary, fontWeight: '600'},
  empty: {padding: 36, alignItems: 'center'},
  emptyText: {color: Colors.subtle, fontSize: 14, fontWeight: '500'},
});

import React from 'react';
import {ScrollView, StyleSheet, Text, View} from 'react-native';
import Icon from 'react-native-vector-icons/MaterialIcons';
import {Colors} from '../theme/colors';

export interface Column<T> {
  key: keyof T;
  label: string;
  flex?: number;
  align?: 'left' | 'right' | 'center';
  render?: (value: T[keyof T], row: T) => string;
}

interface Props<T> {
  columns: Column<T>[];
  data: T[];
  keyExtractor: (item: T, index: number) => string;
}

export default function ReportTable<T>({columns, data, keyExtractor}: Props<T>) {
  return (
    <ScrollView horizontal showsHorizontalScrollIndicator={false}>
      <View style={styles.tableWrap}>
        {/* Header */}
        <View style={styles.headerRow}>
          {columns.map(col => (
            <Text
              key={String(col.key)}
              style={[styles.headerCell, {flex: col.flex ?? 1, textAlign: col.align ?? 'left'}]}>
              {col.label}
            </Text>
          ))}
        </View>

        {/* Rows */}
        {data.map((row, idx) => (
          <View key={keyExtractor(row, idx)} style={[styles.row, idx % 2 === 1 && styles.rowAlt]}>
            {columns.map(col => {
              const raw = row[col.key];
              const text = col.render ? col.render(raw, row) : String(raw ?? '');
              return (
                <Text
                  key={String(col.key)}
                  style={[styles.cell, {flex: col.flex ?? 1, textAlign: col.align ?? 'left'}]}
                  numberOfLines={1}>
                  {text}
                </Text>
              );
            })}
          </View>
        ))}

        {data.length === 0 && (
          <View style={styles.empty}>
            <Icon name="table-rows" size={28} color={Colors.border} />
            <Text style={styles.emptyText}>No data for selected period</Text>
          </View>
        )}
      </View>
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  tableWrap: {minWidth: '100%'},
  headerRow: {
    flexDirection: 'row',
    backgroundColor: Colors.shell,
    paddingVertical: 10,
    paddingHorizontal: 4,
  },
  headerCell: {
    color: 'rgba(255,255,255,0.85)',
    fontSize: 11,
    fontWeight: '700',
    paddingHorizontal: 10,
    letterSpacing: 0.5,
    textTransform: 'uppercase',
  },
  row: {
    flexDirection: 'row',
    paddingVertical: 11,
    paddingHorizontal: 4,
    borderBottomWidth: 1,
    borderBottomColor: Colors.divider,
    backgroundColor: Colors.card,
  },
  rowAlt: {backgroundColor: '#f8f9fb'},
  cell: {fontSize: 13, color: Colors.text, paddingHorizontal: 10, lineHeight: 18},
  empty: {padding: 32, alignItems: 'center', gap: 8},
  emptyText: {color: Colors.subtle, fontSize: 14, fontWeight: '500'},
});

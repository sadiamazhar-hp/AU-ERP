import React, {useMemo} from 'react';
import {ScrollView, StyleSheet, Text, View} from 'react-native';
import Icon from 'react-native-vector-icons/MaterialIcons';
import {Colors} from '../theme/colors';

const BASE_COL_WIDTH = 92;

export interface Column<T> {
  key: keyof T;
  label: string;
  /** Relative width when `width` is not set (multiplied by BASE_COL_WIDTH). */
  flex?: number;
  /** Fixed column width in pixels (overrides flex). */
  width?: number;
  align?: 'left' | 'right' | 'center';
  render?: (value: T[keyof T], row: T) => string;
}

interface Props<T> {
  columns: Column<T>[];
  data: T[];
  keyExtractor: (item: T, index: number) => string;
  emptyMessage?: string;
}

function colWidth<T>(col: Column<T>): number {
  if (col.width != null) return col.width;
  return Math.round((col.flex ?? 1) * BASE_COL_WIDTH);
}

function colLineCount<T>(col: Column<T>): number {
  return (col.flex ?? 1) >= 1.5 ? 2 : 1;
}

export default function ReportTable<T>({columns, data, keyExtractor, emptyMessage}: Props<T>) {
  const tableMinWidth = useMemo(
    () => columns.reduce((sum, col) => sum + colWidth(col), 0),
    [columns],
  );

  return (
    <ScrollView
      horizontal
      nestedScrollEnabled
      showsHorizontalScrollIndicator
      bounces={false}
      directionalLockEnabled>
      <View style={[styles.tableWrap, {minWidth: tableMinWidth}]}>
        <View style={styles.headerRow}>
          {columns.map(col => (
            <Text
              key={String(col.key)}
              style={[
                styles.headerCell,
                styles.colBase,
                {width: colWidth(col), textAlign: col.align ?? 'left'},
              ]}>
              {col.label}
            </Text>
          ))}
        </View>

        {data.map((row, idx) => (
          <View key={keyExtractor(row, idx)} style={[styles.row, idx % 2 === 1 && styles.rowAlt]}>
            {columns.map(col => {
              const raw = row[col.key];
              const text = col.render ? col.render(raw, row) : String(raw ?? '');
              return (
                <Text
                  key={String(col.key)}
                  style={[
                    styles.cell,
                    styles.colBase,
                    {width: colWidth(col), textAlign: col.align ?? 'left'},
                  ]}
                  numberOfLines={colLineCount(col)}>
                  {text}
                </Text>
              );
            })}
          </View>
        ))}

        {data.length === 0 && (
          <View style={[styles.empty, {width: tableMinWidth}]}>
            <Icon name="table-rows" size={28} color={Colors.border} />
            <Text style={styles.emptyText}>{emptyMessage ?? 'No data for selected period'}</Text>
          </View>
        )}
      </View>
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  tableWrap: {},
  colBase: {flexShrink: 0},
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

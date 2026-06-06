import React, {useMemo} from 'react';
import {ScrollView, StyleSheet, Text, View} from 'react-native';
import Icon from 'react-native-vector-icons/MaterialIcons';
import {useOrientationLayout} from '../hooks/useOrientationLayout';
import {Colors} from '../theme/colors';

export interface Column<T> {
  key: keyof T;
  label: string;
  /** Relative width when `width` is not set (multiplied by base column width). */
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

function resolveFlexWeight<T>(col: Column<T>): number {
  return col.width != null ? 0 : col.flex ?? 1;
}

function resolveFixedColWidth<T>(col: Column<T>, baseColWidth: number): number {
  if (col.width != null) return col.width;
  return Math.round((col.flex ?? 1) * baseColWidth);
}

function colLineCount<T>(col: Column<T>, isLandscape: boolean): number {
  if (isLandscape) return (col.flex ?? 1) >= 1.4 ? 2 : 1;
  return (col.flex ?? 1) >= 1.5 ? 2 : 1;
}

export default function ReportTable<T>({columns, data, keyExtractor, emptyMessage}: Props<T>) {
  const {baseColWidth, tableContentWidth, isLandscape} = useOrientationLayout();

  const {columnWidths, tableMinWidth, needsHorizontalScroll} = useMemo(() => {
    const fixedWidths = columns.map(col => resolveFixedColWidth(col, baseColWidth));
    const minWidth = fixedWidths.reduce((sum, w) => sum + w, 0);
    const scroll = minWidth > tableContentWidth;

    if (scroll) {
      return {
        columnWidths: fixedWidths,
        tableMinWidth: minWidth,
        needsHorizontalScroll: true,
      };
    }

    const flexTotal = columns.reduce((sum, col) => sum + resolveFlexWeight(col), 0) || 1;
    const fittedWidths = columns.map((col, index) => {
      if (col.width != null) return col.width;
      const weight = resolveFlexWeight(col);
      if (weight <= 0) return fixedWidths[index];
      return Math.max(Math.floor((weight / flexTotal) * tableContentWidth), isLandscape ? 72 : 64);
    });

    const fittedSum = fittedWidths.reduce((sum, w) => sum + w, 0);
    if (fittedSum > tableContentWidth) {
      return {
        columnWidths: fixedWidths,
        tableMinWidth: minWidth,
        needsHorizontalScroll: true,
      };
    }

    return {
      columnWidths: fittedWidths,
      tableMinWidth: Math.max(fittedSum, tableContentWidth),
      needsHorizontalScroll: false,
    };
  }, [columns, baseColWidth, tableContentWidth, isLandscape]);

  const tableBody = (
    <View
      style={[
        styles.tableWrap,
        needsHorizontalScroll
          ? {minWidth: tableMinWidth}
          : {width: tableContentWidth, alignSelf: 'stretch'},
      ]}>
      <View style={styles.headerRow}>
        {columns.map((col, index) => (
          <Text
            key={String(col.key)}
            style={[
              styles.headerCell,
              styles.colBase,
              {
                width: columnWidths[index],
                textAlign: col.align ?? 'left',
              },
              isLandscape && styles.headerCellLandscape,
            ]}
            numberOfLines={1}
            ellipsizeMode="tail">
            {col.label}
          </Text>
        ))}
      </View>

      {data.map((row, idx) => (
        <View key={keyExtractor(row, idx)} style={[styles.row, idx % 2 === 1 && styles.rowAlt]}>
          {columns.map((col, colIndex) => {
            const raw = row[col.key];
            const text = col.render ? col.render(raw, row) : String(raw ?? '');
            return (
              <Text
                key={String(col.key)}
                style={[
                  styles.cell,
                  styles.colBase,
                  {
                    width: columnWidths[colIndex],
                    textAlign: col.align ?? 'left',
                  },
                  isLandscape && styles.cellLandscape,
                ]}
                numberOfLines={colLineCount(col, isLandscape)}
                ellipsizeMode="tail">
                {text}
              </Text>
            );
          })}
        </View>
      ))}

      {data.length === 0 && (
        <View style={[styles.empty, {width: needsHorizontalScroll ? tableMinWidth : tableContentWidth}]}>
          <Icon name="table-rows" size={28} color={Colors.border} />
          <Text style={styles.emptyText}>{emptyMessage ?? 'No data for selected period'}</Text>
        </View>
      )}
    </View>
  );

  if (!needsHorizontalScroll) {
    return tableBody;
  }

  return (
    <ScrollView
      horizontal
      nestedScrollEnabled
      showsHorizontalScrollIndicator
      bounces={false}
      directionalLockEnabled
      contentContainerStyle={styles.horizontalScrollContent}>
      {tableBody}
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  tableWrap: {
    borderRadius: 8,
    overflow: 'hidden',
  },
  horizontalScrollContent: {
    flexGrow: 1,
  },
  colBase: {flexShrink: 0},
  headerRow: {
    flexDirection: 'row',
    backgroundColor: Colors.shell,
    paddingVertical: 10,
    paddingHorizontal: 2,
  },
  headerCell: {
    color: 'rgba(255,255,255,0.85)',
    fontSize: 11,
    fontWeight: '700',
    paddingHorizontal: 8,
    letterSpacing: 0.5,
    textTransform: 'uppercase',
  },
  headerCellLandscape: {
    fontSize: 10,
    paddingHorizontal: 6,
  },
  row: {
    flexDirection: 'row',
    paddingVertical: 11,
    paddingHorizontal: 2,
    borderBottomWidth: 1,
    borderBottomColor: Colors.divider,
    backgroundColor: Colors.card,
  },
  rowAlt: {backgroundColor: '#f8f9fb'},
  cell: {fontSize: 13, color: Colors.text, paddingHorizontal: 8, lineHeight: 18},
  cellLandscape: {fontSize: 12, lineHeight: 17, paddingHorizontal: 6},
  empty: {padding: 32, alignItems: 'center', gap: 8},
  emptyText: {color: Colors.subtle, fontSize: 14, fontWeight: '500'},
});

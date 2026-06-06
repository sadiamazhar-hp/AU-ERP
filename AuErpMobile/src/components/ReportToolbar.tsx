import React from 'react';
import {StyleSheet, Text, TouchableOpacity, View} from 'react-native';
import Icon from 'react-native-vector-icons/MaterialIcons';
import QuickRangeChips from './QuickRangeChips';
import {formatPeriodLabel} from '../utils/dateRanges';
import {Colors} from '../theme/colors';

interface Props {
  dateFrom: Date | null;
  dateTo: Date | null;
  onQuickRangeChange?: (from: Date, to: Date) => void;
  onFilterPress: () => void;
  loading?: boolean;
  showQuickRanges?: boolean;
  periodLabel?: string;
}

export default function ReportToolbar({
  dateFrom,
  dateTo,
  onQuickRangeChange,
  onFilterPress,
  loading,
  showQuickRanges = true,
  periodLabel,
}: Props) {
  return (
    <View style={styles.container}>
      <View style={styles.topRow}>
        <Text style={styles.period}>{periodLabel ?? formatPeriodLabel(dateFrom, dateTo)}</Text>
        <TouchableOpacity style={styles.filterBtn} onPress={onFilterPress}>
          <Icon name="filter-list" size={16} color={Colors.blue} />
          <Text style={styles.filterTxt}>Filter</Text>
        </TouchableOpacity>
      </View>
      {showQuickRanges && onQuickRangeChange && (
        <QuickRangeChips
          dateFrom={dateFrom}
          dateTo={dateTo}
          onChange={onQuickRangeChange}
          disabled={loading}
        />
      )}
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    backgroundColor: Colors.card,
    borderBottomWidth: 1,
    borderBottomColor: Colors.border,
    paddingHorizontal: 16,
    paddingTop: 10,
    paddingBottom: 12,
    gap: 10,
  },
  topRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  period: {
    fontSize: 13,
    fontWeight: '600',
    color: Colors.text,
    flex: 1,
    marginRight: 12,
  },
  filterBtn: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 4,
    paddingHorizontal: 10,
    paddingVertical: 6,
    borderRadius: 8,
    backgroundColor: Colors.blueLight,
  },
  filterTxt: {
    fontSize: 13,
    fontWeight: '600',
    color: Colors.blue,
  },
});

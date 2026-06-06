import React from 'react';
import {ScrollView, StyleSheet, Text, TouchableOpacity, View} from 'react-native';
import {
  getQuickRangeDates,
  matchesQuickRange,
  QUICK_RANGE_LABELS,
  QUICK_RANGES,
  QuickRange,
} from '../utils/dateRanges';
import {Colors} from '../theme/colors';

interface Props {
  dateFrom: Date | null;
  dateTo: Date | null;
  onChange: (from: Date, to: Date) => void;
  disabled?: boolean;
}

export default function QuickRangeChips({dateFrom, dateTo, onChange, disabled}: Props) {
  function applyRange(range: QuickRange) {
    if (disabled) return;
    const {from, to} = getQuickRangeDates(range);
    onChange(from, to);
  }

  return (
    <ScrollView
      horizontal
      showsHorizontalScrollIndicator={false}
      contentContainerStyle={styles.row}>
      {QUICK_RANGES.map(range => {
        const active = matchesQuickRange(dateFrom, dateTo, range);
        return (
          <TouchableOpacity
            key={range}
            style={[styles.chip, active && styles.chipActive, disabled && styles.chipDisabled]}
            onPress={() => applyRange(range)}
            disabled={disabled}>
            <Text style={[styles.chipText, active && styles.chipTextActive]}>{QUICK_RANGE_LABELS[range]}</Text>
          </TouchableOpacity>
        );
      })}
      <View style={styles.spacer} />
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  row: {
    flexDirection: 'row',
    gap: 8,
    paddingVertical: 2,
  },
  spacer: {width: 4},
  chip: {
    paddingHorizontal: 12,
    paddingVertical: 7,
    borderRadius: 999,
    borderWidth: 1,
    borderColor: Colors.border,
    backgroundColor: Colors.bg,
  },
  chipActive: {
    borderColor: Colors.blue,
    backgroundColor: Colors.blueLight,
  },
  chipDisabled: {
    opacity: 0.5,
  },
  chipText: {
    fontSize: 12,
    fontWeight: '600',
    color: Colors.textSecondary,
  },
  chipTextActive: {
    color: Colors.blue,
  },
});

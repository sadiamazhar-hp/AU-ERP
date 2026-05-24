import React from 'react';
import {StyleSheet, Text, View} from 'react-native';
import {Colors} from '../theme/colors';

interface Props {
  title: string;
  right?: React.ReactNode;
  accent?: string;
}

export default function SectionHeader({title, right, accent = Colors.blue}: Props) {
  return (
    <View style={styles.row}>
      <View style={[styles.accentDot, {backgroundColor: accent}]} />
      <Text style={styles.title}>{title}</Text>
      {right ? <View style={styles.rightSlot}>{right}</View> : null}
    </View>
  );
}

const styles = StyleSheet.create({
  row: {
    flexDirection: 'row',
    alignItems: 'center',
    marginBottom: 12,
    marginTop: 8,
  },
  accentDot: {
    width: 3,
    height: 14,
    borderRadius: 2,
    marginRight: 8,
  },
  title: {
    fontSize: 13,
    fontWeight: '700',
    color: Colors.textSecondary,
    letterSpacing: 0.6,
    textTransform: 'uppercase',
    flex: 1,
  },
  rightSlot: {},
});

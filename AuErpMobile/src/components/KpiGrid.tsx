import React, {useMemo} from 'react';
import {StyleSheet, View} from 'react-native';
import {useOrientationLayout} from '../hooks/useOrientationLayout';
import KpiCard from './KpiCard';

export type KpiItem = React.ComponentProps<typeof KpiCard>;

interface Props {
  items: KpiItem[];
}

export default function KpiGrid({items}: Props) {
  const {columnsPerRow} = useOrientationLayout();

  const rows = useMemo(() => {
    const grouped: KpiItem[][] = [];
    for (let i = 0; i < items.length; i += columnsPerRow) {
      grouped.push(items.slice(i, i + columnsPerRow));
    }
    return grouped;
  }, [items, columnsPerRow]);

  return (
    <View style={styles.grid}>
      {rows.map((row, rowIndex) => (
        <View key={rowIndex} style={styles.row}>
          {row.map((item, colIndex) => (
            <KpiCard key={`${rowIndex}-${colIndex}-${item.label}`} {...item} />
          ))}
          {row.length < columnsPerRow
            ? Array.from({length: columnsPerRow - row.length}).map((_, spacerIndex) => (
                <View key={`spacer-${spacerIndex}`} style={styles.spacer} />
              ))
            : null}
        </View>
      ))}
    </View>
  );
}

const styles = StyleSheet.create({
  grid: {marginBottom: 4},
  row: {flexDirection: 'row', gap: 10, marginBottom: 10},
  spacer: {flex: 1, minWidth: 100},
});

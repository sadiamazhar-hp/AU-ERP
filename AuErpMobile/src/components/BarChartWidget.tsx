import React from 'react';
import {Dimensions, StyleSheet, Text, View} from 'react-native';
import {BarChart} from 'react-native-chart-kit';
import {Colors} from '../theme/colors';

interface Props {
  title?: string;
  labels: string[];
  data: number[];
  color?: string;
  suffix?: string;
  decimalPlaces?: number;
  formatYLabel?: (value: string) => string;
}

const W = Dimensions.get('window').width - 32;

function hasChartData(data: number[]) {
  return data.length > 0 && data.some(v => Number.isFinite(v));
}

export default function BarChartWidget({
  title,
  labels,
  data,
  color = Colors.blue,
  suffix = '',
  decimalPlaces = 0,
  formatYLabel,
}: Props) {
  if (!hasChartData(data) || labels.length === 0) {
    return (
      <View style={styles.container}>
        {title ? <Text style={styles.title}>{title}</Text> : null}
        <View style={styles.empty}>
          <Text style={styles.emptyText}>No data for selected period</Text>
        </View>
      </View>
    );
  }

  return (
    <View style={styles.container}>
      {title ? <Text style={styles.title}>{title}</Text> : null}
      <BarChart
        data={{
          labels,
          datasets: [{data}],
        }}
        width={W}
        height={200}
        yAxisLabel=""
        yAxisSuffix={suffix}
        chartConfig={{
          backgroundColor: Colors.card,
          backgroundGradientFrom: Colors.card,
          backgroundGradientTo: Colors.card,
          decimalPlaces,
          color: () => color,
          labelColor: () => Colors.subtle,
          propsForBackgroundLines: {stroke: Colors.border},
          formatYLabel,
        }}
        style={styles.chart}
        showValuesOnTopOfBars
        fromZero
      />
    </View>
  );
}

const styles = StyleSheet.create({
  container: {backgroundColor: Colors.card, borderRadius: 8, padding: 12, marginBottom: 12, elevation: 1},
  title: {fontSize: 13, fontWeight: '600', color: Colors.text, marginBottom: 8},
  chart: {borderRadius: 8, marginLeft: -12},
  empty: {padding: 32, alignItems: 'center'},
  emptyText: {color: Colors.subtle, fontSize: 14, fontWeight: '500'},
});

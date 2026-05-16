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
}

const W = Dimensions.get('window').width - 32;

export default function BarChartWidget({title, labels, data, color = Colors.blue, suffix = ''}: Props) {
  return (
    <View style={styles.container}>
      {title ? <Text style={styles.title}>{title}</Text> : null}
      <BarChart
        data={{
          labels,
          datasets: [{data: data.length ? data : [0]}],
        }}
        width={W}
        height={200}
        yAxisLabel=""
        yAxisSuffix={suffix}
        chartConfig={{
          backgroundColor: Colors.card,
          backgroundGradientFrom: Colors.card,
          backgroundGradientTo: Colors.card,
          decimalPlaces: 0,
          color: () => color,
          labelColor: () => Colors.subtle,
          propsForBackgroundLines: {stroke: Colors.border},
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
});

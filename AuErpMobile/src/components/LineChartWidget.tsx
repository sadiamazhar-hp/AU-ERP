import React from 'react';
import {Dimensions, StyleSheet, Text, View} from 'react-native';
import {LineChart} from 'react-native-chart-kit';
import {Colors} from '../theme/colors';

interface Dataset {
  label: string;
  data: number[];
  color: string;
}

interface Props {
  title?: string;
  labels: string[];
  datasets: Dataset[];
}

const W = Dimensions.get('window').width - 32;

export default function LineChartWidget({title, labels, datasets}: Props) {
  return (
    <View style={styles.container}>
      {title ? <Text style={styles.title}>{title}</Text> : null}
      <LineChart
        data={{
          labels,
          datasets: datasets.map(ds => ({
            data: ds.data.length ? ds.data : [0],
            color: () => ds.color,
            strokeWidth: 2,
          })),
          legend: datasets.map(ds => ds.label),
        }}
        width={W}
        height={210}
        chartConfig={{
          backgroundColor: Colors.card,
          backgroundGradientFrom: Colors.card,
          backgroundGradientTo: Colors.card,
          decimalPlaces: 0,
          color: () => Colors.blue,
          labelColor: () => Colors.subtle,
          propsForBackgroundLines: {stroke: Colors.border},
          propsForDots: {r: '4'},
        }}
        bezier
        style={styles.chart}
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

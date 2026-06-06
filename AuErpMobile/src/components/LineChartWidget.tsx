import React, {useMemo} from 'react';
import {ScrollView, StyleSheet} from 'react-native';
import {LineChart} from 'react-native-chart-kit';
import ChartCard from './ChartCard';
import {useOrientationLayout} from '../hooks/useOrientationLayout';
import {Colors} from '../theme/colors';

interface Dataset {
  label: string;
  data: number[];
  color: string;
}

interface Props {
  title?: string;
  subtitle?: string;
  labels: string[];
  datasets: Dataset[];
  decimalPlaces?: number;
  formatYLabel?: (value: string) => string;
}

function hasChartData(datasets: Dataset[]) {
  return datasets.some(ds => ds.data.length > 0 && ds.data.some(v => Number.isFinite(v)));
}

function truncateLabel(label: string, max = 7): string {
  return label.length > max ? `${label.slice(0, max)}…` : label;
}

export default function LineChartWidget({
  title,
  subtitle,
  labels,
  datasets,
  decimalPlaces = 0,
  formatYLabel,
}: Props) {
  const {chartContentWidth, isLandscape} = useOrientationLayout();
  const displayLabels = useMemo(() => labels.map(l => truncateLabel(l)), [labels]);
  const labelPitch = isLandscape ? 48 : 56;
  const scrollThreshold = isLandscape ? 8 : 6;
  const chartWidth = Math.max(chartContentWidth, labels.length * labelPitch);
  const scrollable = labels.length > scrollThreshold;
  const empty = !hasChartData(datasets) || labels.length === 0;
  const legend = datasets.map(ds => ({label: ds.label, color: ds.color}));
  const renderWidth = scrollable ? chartWidth : chartContentWidth;
  const chartHeight = isLandscape ? 190 : 210;

  return (
    <ChartCard title={title} subtitle={subtitle} legend={legend} empty={empty}>
      {!empty && (
        <ScrollView horizontal={scrollable} showsHorizontalScrollIndicator={false}>
          <LineChart
            data={{
              labels: displayLabels,
              datasets: datasets.map(ds => ({
                data: ds.data,
                color: () => ds.color,
                strokeWidth: 2,
              })),
              legend: datasets.map(ds => ds.label),
            }}
            width={renderWidth}
            height={chartHeight}
            chartConfig={{
              backgroundColor: Colors.card,
              backgroundGradientFrom: Colors.card,
              backgroundGradientTo: Colors.card,
              decimalPlaces,
              color: () => Colors.blue,
              labelColor: () => Colors.subtle,
              propsForBackgroundLines: {stroke: Colors.border, strokeDasharray: ''},
              propsForDots: {r: '4', strokeWidth: 2},
              propsForLabels: {fontSize: isLandscape ? 9 : 10},
              formatYLabel,
            }}
            bezier
            style={styles.chart}
            fromZero
          />
        </ScrollView>
      )}
    </ChartCard>
  );
}

const styles = StyleSheet.create({
  chart: {borderRadius: 8, marginLeft: -8},
});

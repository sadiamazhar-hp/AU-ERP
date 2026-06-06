import React, {useMemo} from 'react';
import {ScrollView, StyleSheet} from 'react-native';
import {BarChart} from 'react-native-chart-kit';
import ChartCard from './ChartCard';
import {useOrientationLayout} from '../hooks/useOrientationLayout';
import {Colors} from '../theme/colors';

interface Props {
  title?: string;
  subtitle?: string;
  labels: string[];
  data: number[];
  color?: string;
  suffix?: string;
  decimalPlaces?: number;
  formatYLabel?: (value: string) => string;
}

function hasChartData(data: number[]) {
  return data.length > 0 && data.some(v => Number.isFinite(v));
}

function truncateLabel(label: string, max = 7): string {
  return label.length > max ? `${label.slice(0, max)}…` : label;
}

export default function BarChartWidget({
  title,
  subtitle,
  labels,
  data,
  color = Colors.blue,
  suffix = '',
  decimalPlaces = 0,
  formatYLabel,
}: Props) {
  const {chartContentWidth, isLandscape} = useOrientationLayout();
  const displayLabels = useMemo(() => labels.map(l => truncateLabel(l)), [labels]);
  const labelPitch = isLandscape ? 48 : 56;
  const scrollThreshold = isLandscape ? 8 : 6;
  const chartWidth = Math.max(chartContentWidth, labels.length * labelPitch);
  const scrollable = labels.length > scrollThreshold;
  const empty = !hasChartData(data) || labels.length === 0;
  const renderWidth = scrollable ? chartWidth : chartContentWidth;
  const chartHeight = isLandscape ? 180 : 200;

  return (
    <ChartCard title={title} subtitle={subtitle} empty={empty}>
      {!empty && (
        <ScrollView horizontal={scrollable} showsHorizontalScrollIndicator={false}>
          <BarChart
            data={{
              labels: displayLabels,
              datasets: [{data}],
            }}
            width={renderWidth}
            height={chartHeight}
            yAxisLabel=""
            yAxisSuffix={suffix}
            chartConfig={{
              backgroundColor: Colors.card,
              backgroundGradientFrom: Colors.card,
              backgroundGradientTo: Colors.card,
              decimalPlaces,
              color: () => color,
              labelColor: () => Colors.subtle,
              propsForBackgroundLines: {stroke: Colors.border, strokeDasharray: ''},
              propsForLabels: {fontSize: isLandscape ? 9 : 10},
              barPercentage: isLandscape ? 0.55 : 0.65,
              formatYLabel,
            }}
            style={styles.chart}
            showValuesOnTopOfBars
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

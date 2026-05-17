import React, {useCallback, useState} from 'react';
import {RefreshControl, ScrollView, StyleSheet, Text, TouchableOpacity, View} from 'react-native';
import {SafeAreaView} from 'react-native-safe-area-context';
import Icon from 'react-native-vector-icons/MaterialIcons';
import {useFocusEffect} from '@react-navigation/native';
import {getProductionSummary, ProductionSummaryDto} from '../../api/production';
import KpiCard from '../../components/KpiCard';
import BarChartWidget from '../../components/BarChartWidget';
import LineChartWidget from '../../components/LineChartWidget';
import FilterSheet from '../../components/FilterSheet';
import LoadingView from '../../components/LoadingView';
import ErrorView from '../../components/ErrorView';
import SectionHeader from '../../components/SectionHeader';
import {defaultFilter, filterToProductionParams} from '../../utils/reportFilters';
import {Colors} from '../../theme/colors';

export default function ProductionSummaryScreen() {
  const [data, setData] = useState<ProductionSummaryDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [filter, setFilter] = useState(defaultFilter);
  const [showFilter, setShowFilter] = useState(false);

  const load = useCallback(async (f: typeof defaultFilter) => {
    setLoading(true);
    setError(null);
    try {
      const d = await getProductionSummary(filterToProductionParams(f));
      setData(d);
    } catch {
      setError('Failed to load production summary.');
    } finally {
      setLoading(false);
    }
  }, []);

  useFocusEffect(useCallback(() => { load(filter); }, [load, filter]));

  if (loading && !data) return <LoadingView />;
  if (error && !data) return <ErrorView message={error} onRetry={() => load(filter)} />;

  const kpis = data?.kpis;
  const weekly = data?.weeklySeries ?? [];
  const chartLabels = weekly.map(w => w.weekLabel);
  const chartData = weekly.map(w => w.quantityProduced);
  const defectData = weekly.map(w => w.defectPercent);

  return (
    <SafeAreaView style={styles.safe} edges={['bottom']}>
      <View style={styles.toolbar}>
        <Text style={styles.period}>
          {filter.dateFrom ? filter.dateFrom.toLocaleDateString() : 'All time'} –{' '}
          {filter.dateTo ? filter.dateTo.toLocaleDateString() : 'Today'}
        </Text>
        <TouchableOpacity style={styles.filterBtn} onPress={() => setShowFilter(true)}>
          <Icon name="filter-list" size={16} color={Colors.blue} />
          <Text style={styles.filterTxt}>Filter</Text>
        </TouchableOpacity>
      </View>

      <ScrollView
        contentContainerStyle={styles.content}
        refreshControl={<RefreshControl refreshing={loading} onRefresh={() => load(filter)} tintColor={Colors.blue} />}>

        <SectionHeader title="Key Metrics" />
        <View style={styles.kpiRow}>
          <KpiCard label="Batches" value={String(kpis?.totalBatches ?? 0)} accent={Colors.blue} />
          <KpiCard label="Total Qty" value={String(kpis?.totalQuantityProduced ?? 0)} accent={Colors.green} />
        </View>
        <View style={styles.kpiRow}>
          <KpiCard label="Good %" value={`${(kpis?.gradeAPercentage ?? 0).toFixed(1)}%`} accent={Colors.green} />
          <KpiCard label="Defect Rate" value={`${(kpis?.defectRate ?? 0).toFixed(1)}%`} accent={Colors.red} />
        </View>

        <SectionHeader title="Weekly Output" />
        <BarChartWidget labels={chartLabels} data={chartData} color={Colors.blue} decimalPlaces={0} />

        <SectionHeader title="Defect % Trend" />
        <LineChartWidget
          labels={chartLabels}
          datasets={[{label: 'Defect %', data: defectData, color: Colors.red}]}
          decimalPlaces={1}
          formatYLabel={v => `${v}%`}
        />
      </ScrollView>

      <FilterSheet visible={showFilter} values={filter} onApply={setFilter} onClose={() => setShowFilter(false)} showPlant />
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  safe: {flex: 1, backgroundColor: Colors.bg},
  toolbar: {flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center', paddingHorizontal: 16, paddingVertical: 10, backgroundColor: Colors.card, borderBottomWidth: 1, borderBottomColor: Colors.border},
  period: {fontSize: 13, color: Colors.subtle},
  filterBtn: {flexDirection: 'row', alignItems: 'center', gap: 4, paddingHorizontal: 12, paddingVertical: 6, backgroundColor: Colors.blueLight, borderRadius: 6},
  filterTxt: {fontSize: 13, color: Colors.blue, fontWeight: '600'},
  content: {padding: 16},
  kpiRow: {flexDirection: 'row', gap: 10, marginBottom: 10},
});

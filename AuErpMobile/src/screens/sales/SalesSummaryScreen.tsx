import React, {useCallback, useState} from 'react';
import {RefreshControl, ScrollView, StyleSheet, Text, TouchableOpacity, View} from 'react-native';
import {SafeAreaView} from 'react-native-safe-area-context';
import Icon from 'react-native-vector-icons/MaterialIcons';
import {useFocusEffect} from '@react-navigation/native';
import {getSalesSummary, SalesSummaryDto} from '../../api/sales';
import KpiCard from '../../components/KpiCard';
import LineChartWidget from '../../components/LineChartWidget';
import FilterSheet from '../../components/FilterSheet';
import LoadingView from '../../components/LoadingView';
import ErrorView from '../../components/ErrorView';
import SectionHeader from '../../components/SectionHeader';
import {defaultFilter, filterToSalesParams} from '../../utils/reportFilters';
import {Colors} from '../../theme/colors';

const fmtM = (n: number) => `PKR ${(n / 1_000_000).toFixed(2)}M`;
const fmtK = (n: number) => `PKR ${(n / 1_000).toFixed(0)}K`;

export default function SalesSummaryScreen() {
  const [data, setData] = useState<SalesSummaryDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [filter, setFilter] = useState(defaultFilter);
  const [showFilter, setShowFilter] = useState(false);

  const load = useCallback(async (f: typeof defaultFilter) => {
    setLoading(true);
    setError(null);
    try {
      const d = await getSalesSummary(filterToSalesParams(f));
      setData(d);
    } catch {
      setError('Failed to load sales summary.');
    } finally {
      setLoading(false);
    }
  }, []);

  useFocusEffect(useCallback(() => { load(filter); }, [load, filter]));

  if (loading && !data) return <LoadingView />;
  if (error && !data) return <ErrorView message={error} onRetry={() => load(filter)} />;

  const kpis = data?.kpis;
  const monthly = data?.monthlySeries ?? [];
  const labels = monthly.map(m => m.month);
  const revData = monthly.map(m => m.revenue);
  const colData = monthly.map(m => m.collected);

  return (
    <SafeAreaView style={styles.safe} edges={['bottom']}>
      <View style={styles.toolbar}>
        <Text style={styles.period}>
          {filter.dateFrom ? filter.dateFrom.toLocaleDateString() : 'All time'} – {filter.dateTo ? filter.dateTo.toLocaleDateString() : 'Today'}
        </Text>
        <TouchableOpacity style={styles.filterBtn} onPress={() => setShowFilter(true)}>
          <Icon name="filter-list" size={16} color={Colors.blue} />
          <Text style={styles.filterTxt}>Filter</Text>
        </TouchableOpacity>
      </View>

      <ScrollView
        contentContainerStyle={styles.content}
        refreshControl={<RefreshControl refreshing={loading} onRefresh={() => load(filter)} tintColor={Colors.blue} />}>

        <SectionHeader title="Revenue & Collections" />
        <View style={styles.kpiRow}>
          <KpiCard label="Revenue" value={fmtM(kpis?.totalRevenue ?? 0)} accent={Colors.blue} />
          <KpiCard label="Collected" value={fmtM(kpis?.totalCollected ?? 0)} accent={Colors.green} />
        </View>
        <View style={styles.kpiRow}>
          <KpiCard label="Outstanding" value={fmtK(kpis?.totalOutstanding ?? 0)} accent={Colors.orange} />
          <KpiCard label="Invoices" value={String(kpis?.totalInvoices ?? 0)} accent={Colors.accent} />
        </View>
        <View style={styles.kpiRow}>
          <KpiCard label="Paid" value={String(kpis?.paidInvoices ?? 0)} accent={Colors.green} />
          <KpiCard label="Unpaid" value={String(kpis?.unpaidInvoices ?? 0)} accent={Colors.red} />
        </View>
        <View style={styles.kpiRow}>
          <KpiCard label="Avg invoice" value={fmtK(kpis?.averageInvoiceValue ?? 0)} accent={Colors.blue} />
          <KpiCard label="Returns" value={String(kpis?.totalReturns ?? 0)} accent={Colors.subtle} />
        </View>

        <SectionHeader title="Monthly Trend" />
        <LineChartWidget
          labels={labels}
          datasets={[
            {label: 'Revenue', data: revData, color: Colors.blue},
            {label: 'Collected', data: colData, color: Colors.green},
          ]}
          decimalPlaces={0}
        />
      </ScrollView>

      <FilterSheet visible={showFilter} values={filter} onApply={setFilter} onClose={() => setShowFilter(false)} showCustomer showProduct />
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

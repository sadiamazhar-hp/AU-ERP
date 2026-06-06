import React, {useCallback, useState} from 'react';
import {RefreshControl, ScrollView, StyleSheet, View} from 'react-native';
import ScreenSafeArea from '../../components/ScreenSafeArea';
import {useFocusEffect} from '@react-navigation/native';
import {getSalesSummary, SalesSummaryDto} from '../../api/sales';
import LineChartWidget from '../../components/LineChartWidget';
import FilterSheet from '../../components/FilterSheet';
import KpiGrid from '../../components/KpiGrid';
import LoadingView from '../../components/LoadingView';
import ErrorView from '../../components/ErrorView';
import ReportToolbar from '../../components/ReportToolbar';
import SectionHeader from '../../components/SectionHeader';
import {defaultFilter, filterToSalesParams} from '../../utils/reportFilters';
import {formatPkr, formatPkrAxis} from '../../utils/currency';
import {getPeriodSubtitle} from '../../utils/dateRanges';
import {Colors} from '../../theme/colors';

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

  function handleQuickRange(from: Date, to: Date) {
    setFilter(prev => ({...prev, dateFrom: from, dateTo: to}));
  }

  if (loading && !data) return <LoadingView />;
  if (error && !data) return <ErrorView message={error} onRetry={() => load(filter)} />;

  const kpis = data?.kpis;
  const monthly = data?.monthlySeries ?? [];
  const labels = monthly.map(m => m.month);
  const revData = monthly.map(m => m.revenue);
  const colData = monthly.map(m => m.collected);
  const periodSub = getPeriodSubtitle(filter.dateFrom, filter.dateTo);

  return (
    <ScreenSafeArea style={styles.safe}>
      <ReportToolbar
        dateFrom={filter.dateFrom}
        dateTo={filter.dateTo}
        onQuickRangeChange={handleQuickRange}
        onFilterPress={() => setShowFilter(true)}
        loading={loading}
      />

      <ScrollView
        contentContainerStyle={styles.content}
        refreshControl={<RefreshControl refreshing={loading} onRefresh={() => load(filter)} tintColor={Colors.blue} />}>

        <SectionHeader title="Revenue & Collections" />
        <KpiGrid
          items={[
            {label: 'Revenue', value: formatPkr(kpis?.totalRevenue ?? 0), accent: Colors.blue, sub: periodSub},
            {label: 'Collected', value: formatPkr(kpis?.totalCollected ?? 0), accent: Colors.green, sub: periodSub},
            {label: 'Outstanding', value: formatPkr(kpis?.totalOutstanding ?? 0), accent: Colors.orange, sub: periodSub},
            {label: 'Invoices', value: String(kpis?.totalInvoices ?? 0), accent: Colors.accent, sub: periodSub},
            {label: 'Paid', value: String(kpis?.paidInvoices ?? 0), accent: Colors.green, sub: periodSub},
            {label: 'Unpaid', value: String(kpis?.unpaidInvoices ?? 0), accent: Colors.red, sub: periodSub},
            {label: 'Avg invoice', value: formatPkr(kpis?.averageInvoiceValue ?? 0), accent: Colors.blue, sub: periodSub},
            {label: 'Returns', value: String(kpis?.totalReturns ?? 0), accent: Colors.subtle, sub: periodSub},
          ]}
        />

        <SectionHeader title="Monthly Trend" />
        <LineChartWidget
          title="Revenue vs Collections"
          subtitle={periodSub}
          labels={labels}
          datasets={[
            {label: 'Revenue', data: revData, color: Colors.blue},
            {label: 'Collected', data: colData, color: Colors.green},
          ]}
          decimalPlaces={0}
          formatYLabel={v => formatPkrAxis(Number(v))}
        />
      </ScrollView>

      <FilterSheet visible={showFilter} values={filter} onApply={setFilter} onClose={() => setShowFilter(false)} showCustomer showProduct />
    </ScreenSafeArea>
  );
}

const styles = StyleSheet.create({
  safe: {flex: 1, backgroundColor: Colors.bg},
  content: {padding: 16},
});

import React, {useCallback, useState} from 'react';
import {RefreshControl, ScrollView, StyleSheet} from 'react-native';
import ScreenSafeArea from '../../components/ScreenSafeArea';
import {useFocusEffect} from '@react-navigation/native';
import {getProductionSummary, ProductionSummaryDto} from '../../api/production';
import BarChartWidget from '../../components/BarChartWidget';
import LineChartWidget from '../../components/LineChartWidget';
import FilterSheet from '../../components/FilterSheet';
import KpiGrid from '../../components/KpiGrid';
import LoadingView from '../../components/LoadingView';
import ErrorView from '../../components/ErrorView';
import ReportToolbar from '../../components/ReportToolbar';
import SectionHeader from '../../components/SectionHeader';
import {defaultFilter, filterToProductionParams} from '../../utils/reportFilters';
import {getPeriodSubtitle} from '../../utils/dateRanges';
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

  function handleQuickRange(from: Date, to: Date) {
    setFilter(prev => ({...prev, dateFrom: from, dateTo: to}));
  }

  if (loading && !data) return <LoadingView />;
  if (error && !data) return <ErrorView message={error} onRetry={() => load(filter)} />;

  const kpis = data?.kpis;
  const weekly = data?.weeklySeries ?? [];
  const chartLabels = weekly.map(w => w.weekLabel);
  const chartData = weekly.map(w => w.quantityProduced);
  const defectData = weekly.map(w => w.defectPercent);
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

        <SectionHeader title="Key Metrics" />
        <KpiGrid
          items={[
            {label: 'Batches', value: String(kpis?.totalBatches ?? 0), accent: Colors.blue, sub: periodSub},
            {label: 'Total Qty', value: String(kpis?.totalQuantityProduced ?? 0), accent: Colors.green, sub: periodSub},
            {label: 'Good %', value: `${(kpis?.gradeAPercentage ?? 0).toFixed(1)}%`, accent: Colors.green, sub: periodSub},
            {label: 'Defect Rate', value: `${(kpis?.defectRate ?? 0).toFixed(1)}%`, accent: Colors.red, sub: periodSub},
          ]}
        />

        <SectionHeader title="Weekly Output" />
        <BarChartWidget
          title="Quantity produced"
          subtitle={periodSub}
          labels={chartLabels}
          data={chartData}
          color={Colors.blue}
          decimalPlaces={0}
        />

        <SectionHeader title="Defect % Trend" />
        <LineChartWidget
          title="Defect rate"
          subtitle={periodSub}
          labels={chartLabels}
          datasets={[{label: 'Defect %', data: defectData, color: Colors.red}]}
          decimalPlaces={1}
          formatYLabel={v => `${v}%`}
        />
      </ScrollView>

      <FilterSheet visible={showFilter} values={filter} onApply={setFilter} onClose={() => setShowFilter(false)} showPlant />
    </ScreenSafeArea>
  );
}

const styles = StyleSheet.create({
  safe: {flex: 1, backgroundColor: Colors.bg},
  content: {padding: 16},
});

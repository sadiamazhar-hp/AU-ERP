import React, {useCallback, useMemo, useState} from 'react';
import {ScrollView, StyleSheet, Text, TouchableOpacity, View} from 'react-native';
import ScreenSafeArea from '../../components/ScreenSafeArea';
import Icon from 'react-native-vector-icons/MaterialIcons';
import {useFocusEffect} from '@react-navigation/native';
import {getDailyProduction, DailyProductionRow, PagedResult} from '../../api/production';
import BarChartWidget from '../../components/BarChartWidget';
import KpiGrid from '../../components/KpiGrid';
import ReportTable, {Column} from '../../components/ReportTable';
import ReportToolbar from '../../components/ReportToolbar';
import SearchBar from '../../components/SearchBar';
import FilterSheet from '../../components/FilterSheet';
import LoadingView from '../../components/LoadingView';
import ErrorView from '../../components/ErrorView';
import SectionHeader from '../../components/SectionHeader';
import {defaultFilter, filterToProductionParams} from '../../utils/reportFilters';
import {getPeriodSubtitle} from '../../utils/dateRanges';
import {filterRowsBySearch} from '../../utils/tableSearch';
import {Colors} from '../../theme/colors';

const PAGE = 20;
const fmtDate = (s: string) => (s ? new Date(s).toLocaleDateString() : '');

const COLS: Column<DailyProductionRow>[] = [
  {key: 'grDate', label: 'Date', flex: 1.2, render: v => fmtDate(String(v))},
  {key: 'materialDescription', label: 'Material', flex: 2},
  {key: 'batchNumber', label: 'Batch', flex: 1.2},
  {key: 'quantityProduced', label: 'Qty', flex: 0.7, align: 'right'},
  {key: 'goodQty', label: 'Good', flex: 0.7, align: 'right'},
  {key: 'defectiveQty', label: 'Defect', flex: 0.7, align: 'right'},
  {key: 'defectPercent', label: 'Def %', flex: 0.7, align: 'right', render: v => `${Number(v).toFixed(1)}%`},
];

const SEARCH_KEYS: (keyof DailyProductionRow)[] = ['batchNumber', 'materialNumber', 'materialDescription'];

export default function DailyProductionScreen() {
  const [result, setResult] = useState<PagedResult<DailyProductionRow> | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [filter, setFilter] = useState(defaultFilter);
  const [showFilter, setShowFilter] = useState(false);
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState('');

  const load = useCallback(async (f: typeof defaultFilter, p: number) => {
    setLoading(true);
    setError(null);
    try {
      const data = await getDailyProduction({...filterToProductionParams(f), page: p, pageSize: PAGE});
      setResult(data);
    } catch {
      setError('Failed to load production data.');
    } finally {
      setLoading(false);
    }
  }, []);

  useFocusEffect(useCallback(() => { load(filter, page); }, [load, filter, page]));

  function handleQuickRange(from: Date, to: Date) {
    setPage(1);
    setFilter(prev => ({...prev, dateFrom: from, dateTo: to}));
  }

  const filteredRows = useMemo(
    () => filterRowsBySearch(result?.items ?? [], search, SEARCH_KEYS),
    [result?.items, search],
  );

  const pageKpis = useMemo(() => {
    const rows = result?.items ?? [];
    const totalQty = rows.reduce((s, r) => s + r.quantityProduced, 0);
    const goodQty = rows.reduce((s, r) => s + r.goodQty, 0);
    const defectiveQty = rows.reduce((s, r) => s + r.defectiveQty, 0);
    const defectPercent = totalQty > 0 ? (defectiveQty / totalQty) * 100 : 0;
    return {totalQty, goodQty, defectiveQty, defectPercent};
  }, [result?.items]);

  const chartRows = useMemo(
    () => (result?.items ?? []).slice(0, 8),
    [result?.items],
  );
  const periodSub = getPeriodSubtitle(filter.dateFrom, filter.dateTo);

  if (loading && !result) return <LoadingView />;
  if (error && !result) return <ErrorView message={error} onRetry={() => load(filter, page)} />;

  return (
    <ScreenSafeArea style={styles.safe}>
      <ReportToolbar
        dateFrom={filter.dateFrom}
        dateTo={filter.dateTo}
        onQuickRangeChange={handleQuickRange}
        onFilterPress={() => setShowFilter(true)}
        loading={loading}
      />

      <ScrollView nestedScrollEnabled contentContainerStyle={styles.content}>
        <SectionHeader title={`Production lines (${result?.totalCount ?? 0})`} />
        <KpiGrid
          items={[
            {label: 'Total qty', value: String(pageKpis.totalQty), sub: 'current view', accent: Colors.blue},
            {label: 'Good qty', value: String(pageKpis.goodQty), sub: 'current view', accent: Colors.green},
            {label: 'Defective', value: String(pageKpis.defectiveQty), sub: 'current view', accent: Colors.red},
            {label: 'Defect %', value: `${pageKpis.defectPercent.toFixed(1)}%`, sub: 'current view', accent: Colors.orange},
          ]}
        />

        {chartRows.length > 0 && (
          <BarChartWidget
            title="Qty by batch"
            subtitle={`${periodSub} · current page`}
            labels={chartRows.map(r => r.batchNumber || '—')}
            data={chartRows.map(r => r.quantityProduced)}
            color={Colors.blue}
            decimalPlaces={0}
          />
        )}

        <SearchBar value={search} onChangeText={setSearch} placeholder="Search production…" />
        <View style={styles.tableWrap}>
          <ReportTable columns={COLS} data={filteredRows} keyExtractor={(_, i) => String(i)} emptyMessage={search ? 'No matches on this page' : undefined} />
        </View>

        {(result?.totalPages ?? 0) > 1 && (
          <View style={styles.pagination}>
            <TouchableOpacity disabled={page <= 1} onPress={() => setPage(p => p - 1)} style={[styles.pageBtn, page <= 1 && styles.pageBtnDisabled]}>
              <Icon name="chevron-left" size={20} color={page <= 1 ? Colors.subtle : Colors.blue} />
            </TouchableOpacity>
            <Text style={styles.pageText}>{page} / {result?.totalPages}</Text>
            <TouchableOpacity disabled={page >= (result?.totalPages ?? 1)} onPress={() => setPage(p => p + 1)} style={[styles.pageBtn, page >= (result?.totalPages ?? 1) && styles.pageBtnDisabled]}>
              <Icon name="chevron-right" size={20} color={page >= (result?.totalPages ?? 1) ? Colors.subtle : Colors.blue} />
            </TouchableOpacity>
          </View>
        )}
      </ScrollView>

      <FilterSheet visible={showFilter} values={filter} onApply={v => { setFilter(v); setPage(1); setSearch(''); }} onClose={() => setShowFilter(false)} showPlant />
    </ScreenSafeArea>
  );
}

const styles = StyleSheet.create({
  safe: {flex: 1, backgroundColor: Colors.bg},
  content: {padding: 16},
  tableWrap: {backgroundColor: Colors.card, borderRadius: 8, overflow: 'hidden', elevation: 1, marginBottom: 8},
  pagination: {flexDirection: 'row', alignItems: 'center', justifyContent: 'center', gap: 16, padding: 16},
  pageBtn: {padding: 4},
  pageBtnDisabled: {opacity: 0.4},
  pageText: {fontSize: 13, color: Colors.text, fontWeight: '600'},
});

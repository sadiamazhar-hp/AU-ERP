import React, {useCallback, useMemo, useState} from 'react';
import {ScrollView, StyleSheet, Text, TouchableOpacity, View} from 'react-native';
import ScreenSafeArea from '../../components/ScreenSafeArea';
import Icon from 'react-native-vector-icons/MaterialIcons';
import {useFocusEffect} from '@react-navigation/native';
import {getBatches, BatchTrackingRow, PagedResult} from '../../api/production';
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

const fmtDate = (s: string) => (s ? new Date(s).toLocaleDateString() : '');

const COLS: Column<BatchTrackingRow>[] = [
  {key: 'batchNumber', label: 'Batch', flex: 1.2},
  {key: 'materialDescription', label: 'Material', flex: 2},
  {key: 'grDate', label: 'Date', flex: 1, render: v => fmtDate(String(v))},
  {key: 'quantityProduced', label: 'Prod', flex: 0.8, align: 'right'},
  {key: 'goodPercent', label: 'Good %', flex: 0.8, align: 'right', render: v => `${Number(v).toFixed(0)}%`},
  {key: 'gradeA', label: 'A', flex: 0.6, align: 'right'},
  {key: 'defects', label: 'Def', flex: 0.6, align: 'right'},
];

const SEARCH_KEYS: (keyof BatchTrackingRow)[] = ['batchNumber', 'materialNumber', 'materialDescription'];

export default function BatchTrackingScreen() {
  const [result, setResult] = useState<PagedResult<BatchTrackingRow> | null>(null);
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
      const d = await getBatches({...filterToProductionParams(f), page: p, pageSize: 20});
      setResult(d);
    } catch {
      setError('Failed to load batch data.');
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

  const avgGood = useMemo(() => {
    const rows = result?.items ?? [];
    if (!rows.length) return 0;
    return rows.reduce((s, r) => s + r.goodPercent, 0) / rows.length;
  }, [result?.items]);

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
        <SectionHeader title={`Batch KPIs (${result?.totalCount ?? 0})`} />
        <KpiGrid
          items={[
            {label: 'Total batches', value: String(result?.totalCount ?? 0), sub: periodSub, accent: Colors.blue},
            {label: 'Avg Good %', value: `${avgGood.toFixed(1)}%`, sub: 'this page', accent: Colors.green},
          ]}
        />

        <SearchBar value={search} onChangeText={setSearch} placeholder="Search batches…" />
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

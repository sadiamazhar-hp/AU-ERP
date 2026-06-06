import React, {useCallback, useMemo, useState} from 'react';
import {ScrollView, StyleSheet, Text, TouchableOpacity, View} from 'react-native';
import ScreenSafeArea from '../../components/ScreenSafeArea';
import Icon from 'react-native-vector-icons/MaterialIcons';
import {useFocusEffect} from '@react-navigation/native';
import {getSalesReturns, SalesReturnsDto, SalesReturnRow} from '../../api/sales';
import KpiGrid from '../../components/KpiGrid';
import ReportTable, {Column} from '../../components/ReportTable';
import ReportToolbar from '../../components/ReportToolbar';
import SearchBar from '../../components/SearchBar';
import FilterSheet from '../../components/FilterSheet';
import LoadingView from '../../components/LoadingView';
import ErrorView from '../../components/ErrorView';
import SectionHeader from '../../components/SectionHeader';
import {defaultFilter, filterToSalesParams} from '../../utils/reportFilters';
import {filterRowsBySearch} from '../../utils/tableSearch';
import {formatPkr} from '../../utils/currency';
import {getPeriodSubtitle} from '../../utils/dateRanges';
import {Colors} from '../../theme/colors';

const fmtDate = (s: string) => (s ? new Date(s).toLocaleDateString() : '');

const COLS: Column<SalesReturnRow>[] = [
  {key: 'returnNumber', label: 'Return #', flex: 1.2},
  {key: 'returnDate', label: 'Date', flex: 1, render: v => fmtDate(String(v))},
  {key: 'customerName', label: 'Customer', flex: 1.6},
  {key: 'invoiceNumber', label: 'Invoice', flex: 1.1},
  {key: 'returnReason', label: 'Reason', flex: 1.4},
  {key: 'invoiceTotal', label: 'Inv total', flex: 0.9, align: 'right', render: v => formatPkr(Number(v))},
  {key: 'creditAmount', label: 'Credit', flex: 0.9, align: 'right', render: v => (v != null ? formatPkr(Number(v)) : '—')},
  {key: 'hasQualityInspection', label: 'QI', flex: 0.6, render: v => (v ? 'Yes' : 'No')},
  {key: 'hasCreditMemo', label: 'CM', flex: 0.6, render: v => (v ? 'Yes' : 'No')},
];

const SEARCH_KEYS: (keyof SalesReturnRow)[] = ['returnNumber', 'customerName', 'invoiceNumber', 'returnReason'];

export default function SalesReturnsScreen() {
  const [data, setData] = useState<SalesReturnsDto | null>(null);
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
      const d = await getSalesReturns({...filterToSalesParams(f), page: p, pageSize: 20});
      setData(d);
    } catch {
      setError('Failed to load returns data.');
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
    () => filterRowsBySearch(data?.rows.items ?? [], search, SEARCH_KEYS),
    [data?.rows.items, search],
  );

  if (loading && !data) return <LoadingView />;
  if (error && !data) return <ErrorView message={error} onRetry={() => load(filter, page)} />;

  const kpis = data?.kpis;
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
      <ScrollView nestedScrollEnabled contentContainerStyle={styles.content}>
        <SectionHeader title={`Return KPIs (${data?.rows.totalCount ?? 0})`} />
        <KpiGrid
          items={[
            {label: 'Total Returns', value: String(kpis?.totalReturns ?? 0), sub: periodSub, accent: Colors.red},
            {label: 'Return Value', value: formatPkr(kpis?.totalReturnValue ?? 0), sub: periodSub, accent: Colors.orange},
            {label: 'Pending QI', value: String(kpis?.pendingQualityInspection ?? 0), sub: periodSub, accent: Colors.orange},
            {label: 'Credit memos', value: String(kpis?.creditMemoIssued ?? 0), sub: periodSub, accent: Colors.green},
            {label: 'Credit issued', value: formatPkr(kpis?.totalCreditIssued ?? 0), sub: periodSub, accent: Colors.blue},
          ]}
        />

        <SectionHeader title="Return List" />
        <SearchBar value={search} onChangeText={setSearch} placeholder="Search returns…" />
        <View style={styles.tableWrap}>
          <ReportTable columns={COLS} data={filteredRows} keyExtractor={(_, i) => String(i)} emptyMessage={search ? 'No matches on this page' : undefined} />
        </View>

        {(data?.rows.totalPages ?? 0) > 1 && (
          <View style={styles.pagination}>
            <TouchableOpacity disabled={page <= 1} onPress={() => setPage(p => p - 1)} style={[styles.pageBtn, page <= 1 && styles.pageBtnDisabled]}>
              <Icon name="chevron-left" size={20} color={page <= 1 ? Colors.subtle : Colors.blue} />
            </TouchableOpacity>
            <Text style={styles.pageText}>{page} / {data?.rows.totalPages}</Text>
            <TouchableOpacity disabled={page >= (data?.rows.totalPages ?? 1)} onPress={() => setPage(p => p + 1)} style={[styles.pageBtn, page >= (data?.rows.totalPages ?? 1) && styles.pageBtnDisabled]}>
              <Icon name="chevron-right" size={20} color={page >= (data?.rows.totalPages ?? 1) ? Colors.subtle : Colors.blue} />
            </TouchableOpacity>
          </View>
        )}
      </ScrollView>
      <FilterSheet visible={showFilter} values={filter} onApply={v => { setFilter(v); setPage(1); setSearch(''); }} onClose={() => setShowFilter(false)} showCustomer />
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

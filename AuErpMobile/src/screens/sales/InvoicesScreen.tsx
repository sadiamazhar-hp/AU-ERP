import React, {useCallback, useMemo, useState} from 'react';
import {ScrollView, StyleSheet, Text, TouchableOpacity, View} from 'react-native';
import ScreenSafeArea from '../../components/ScreenSafeArea';
import Icon from 'react-native-vector-icons/MaterialIcons';
import {useFocusEffect} from '@react-navigation/native';
import {getInvoices, InvoiceRow, PagedResult} from '../../api/sales';
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

const COLS: Column<InvoiceRow>[] = [
  {key: 'invoiceNumber', label: 'Invoice #', flex: 1.3},
  {key: 'documentDate', label: 'Date', flex: 1, render: v => fmtDate(String(v))},
  {key: 'customerName', label: 'Customer', flex: 2},
  {key: 'totalAmount', label: 'Total', flex: 1, align: 'right', render: v => formatPkr(Number(v))},
  {key: 'status', label: 'Status', flex: 1.1},
];

const SEARCH_KEYS: (keyof InvoiceRow)[] = ['invoiceNumber', 'customerName', 'status'];

export default function InvoicesScreen() {
  const [result, setResult] = useState<PagedResult<InvoiceRow> | null>(null);
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
      const d = await getInvoices({...filterToSalesParams(f), page: p, pageSize: 20});
      setResult(d);
    } catch {
      setError('Failed to load invoices.');
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
    const grandTotal = rows.reduce((s, r) => s + r.totalAmount, 0);
    const open = rows.filter(r => r.status === 'Open').length;
    const collected = rows.filter(r => r.status === 'Collected').length;
    return {count: rows.length, grandTotal, open, collected};
  }, [result?.items]);

  if (loading && !result) return <LoadingView />;
  if (error && !result) return <ErrorView message={error} onRetry={() => load(filter, page)} />;

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
        <SectionHeader title={`Invoices (${result?.totalCount ?? 0})`} />
        <KpiGrid
          items={[
            {label: 'Rows', value: String(pageKpis.count), sub: 'current view', accent: Colors.blue},
            {label: 'Grand total', value: formatPkr(pageKpis.grandTotal), sub: periodSub, accent: Colors.green},
            {label: 'Open', value: String(pageKpis.open), sub: 'current view', accent: Colors.orange},
            {label: 'Collected', value: String(pageKpis.collected), sub: 'current view', accent: Colors.green},
          ]}
        />

        <SearchBar value={search} onChangeText={setSearch} placeholder="Search invoices…" />
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
      <FilterSheet visible={showFilter} values={filter} onApply={v => { setFilter(v); setPage(1); setSearch(''); }} onClose={() => setShowFilter(false)} showCustomer showProduct />
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

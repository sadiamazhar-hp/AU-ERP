import React, {useCallback, useMemo, useState} from 'react';
import {ScrollView, StyleSheet, Text, TouchableOpacity, View} from 'react-native';
import {SafeAreaView} from 'react-native-safe-area-context';
import Icon from 'react-native-vector-icons/MaterialIcons';
import {useFocusEffect} from '@react-navigation/native';
import {getProductSales, ProductSalesRow, PagedResult} from '../../api/sales';
import KpiCard from '../../components/KpiCard';
import BarChartWidget from '../../components/BarChartWidget';
import ReportTable, {Column} from '../../components/ReportTable';
import SearchBar from '../../components/SearchBar';
import FilterSheet from '../../components/FilterSheet';
import LoadingView from '../../components/LoadingView';
import ErrorView from '../../components/ErrorView';
import SectionHeader from '../../components/SectionHeader';
import {defaultFilter, filterToSalesParams} from '../../utils/reportFilters';
import {filterRowsBySearch} from '../../utils/tableSearch';
import {Colors} from '../../theme/colors';

const fmtAmt = (n: number) => `${(n / 1000).toFixed(0)}K`;

const COLS: Column<ProductSalesRow>[] = [
  {key: 'materialNumber', label: 'Mat #', flex: 0.9},
  {key: 'materialDescription', label: 'Material', flex: 2},
  {key: 'totalQuantity', label: 'Qty', flex: 0.8, align: 'right'},
  {key: 'unitOfMeasure', label: 'UoM', flex: 0.6, align: 'center'},
  {key: 'totalRevenue', label: 'Revenue', flex: 1.1, align: 'right', render: v => fmtAmt(Number(v))},
  {key: 'invoiceCount', label: 'Invoices', flex: 0.8, align: 'right'},
];

const SEARCH_KEYS: (keyof ProductSalesRow)[] = ['materialNumber', 'materialDescription'];

export default function ProductSalesScreen() {
  const [result, setResult] = useState<PagedResult<ProductSalesRow> | null>(null);
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
      const d = await getProductSales({...filterToSalesParams(f), page: p, pageSize: 20});
      setResult(d);
    } catch {
      setError('Failed to load product sales data.');
    } finally {
      setLoading(false);
    }
  }, []);

  useFocusEffect(useCallback(() => { load(filter, page); }, [load, filter, page]));

  const filteredRows = useMemo(
    () => filterRowsBySearch(result?.items ?? [], search, SEARCH_KEYS),
    [result?.items, search],
  );

  const pageKpis = useMemo(() => {
    const rows = result?.items ?? [];
    const totalQty = rows.reduce((s, r) => s + r.totalQuantity, 0);
    const totalRevenue = rows.reduce((s, r) => s + r.totalRevenue, 0);
    const top = [...rows].sort((a, b) => b.totalRevenue - a.totalRevenue)[0];
    return {totalQty, totalRevenue, top};
  }, [result?.items]);

  const topProducts = useMemo(
    () => [...(result?.items ?? [])].sort((a, b) => b.totalRevenue - a.totalRevenue).slice(0, 5),
    [result?.items],
  );

  if (loading && !result) return <LoadingView />;
  if (error && !result) return <ErrorView message={error} onRetry={() => load(filter, page)} />;

  return (
    <SafeAreaView style={styles.safe} edges={['bottom']}>
      <View style={styles.toolbar}>
        <Text style={styles.count}>{result?.totalCount ?? 0} products</Text>
        <TouchableOpacity style={styles.filterBtn} onPress={() => setShowFilter(true)}>
          <Icon name="filter-list" size={16} color={Colors.blue} />
          <Text style={styles.filterTxt}>Filter</Text>
        </TouchableOpacity>
      </View>
      <ScrollView nestedScrollEnabled contentContainerStyle={styles.content}>
        <SectionHeader title="Product KPIs (this page)" />
        <View style={styles.kpiRow}>
          <KpiCard label="Total qty" value={String(pageKpis.totalQty)} accent={Colors.blue} />
          <KpiCard label="Revenue" value={fmtAmt(pageKpis.totalRevenue)} accent={Colors.green} />
        </View>
        <View style={styles.kpiRow}>
          <KpiCard label="Top SKU" value={pageKpis.top?.materialNumber ?? '—'} accent={Colors.accent} />
          <View style={{flex: 1, minWidth: 140}} />
        </View>

        {topProducts.length > 0 && (
          <BarChartWidget
            title="Top 5 products (this page)"
            labels={topProducts.map(p => p.materialNumber)}
            data={topProducts.map(p => p.totalRevenue)}
            color={Colors.green}
            decimalPlaces={0}
          />
        )}

        <SearchBar value={search} onChangeText={setSearch} placeholder="Search products…" />
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
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  safe: {flex: 1, backgroundColor: Colors.bg},
  toolbar: {flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center', paddingHorizontal: 16, paddingVertical: 10, backgroundColor: Colors.card, borderBottomWidth: 1, borderBottomColor: Colors.border},
  count: {fontSize: 13, color: Colors.subtle},
  filterBtn: {flexDirection: 'row', alignItems: 'center', gap: 4, paddingHorizontal: 12, paddingVertical: 6, backgroundColor: Colors.blueLight, borderRadius: 6},
  filterTxt: {fontSize: 13, color: Colors.blue, fontWeight: '600'},
  content: {padding: 16},
  kpiRow: {flexDirection: 'row', gap: 10, marginBottom: 10},
  tableWrap: {backgroundColor: Colors.card, borderRadius: 8, overflow: 'hidden', elevation: 1, marginBottom: 8},
  pagination: {flexDirection: 'row', alignItems: 'center', justifyContent: 'center', gap: 16, padding: 16},
  pageBtn: {padding: 4},
  pageBtnDisabled: {opacity: 0.4},
  pageText: {fontSize: 13, color: Colors.text, fontWeight: '600'},
});

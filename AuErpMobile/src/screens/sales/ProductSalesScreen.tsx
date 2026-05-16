import React, {useCallback, useState} from 'react';
import {ScrollView, StyleSheet, Text, TouchableOpacity, View} from 'react-native';
import {SafeAreaView} from 'react-native-safe-area-context';
import Icon from 'react-native-vector-icons/MaterialIcons';
import {useFocusEffect} from '@react-navigation/native';
import {getProductSales, ProductSalesRow, PagedResult} from '../../api/sales';
import ReportTable, {Column} from '../../components/ReportTable';
import FilterSheet, {FilterValues} from '../../components/FilterSheet';
import LoadingView from '../../components/LoadingView';
import ErrorView from '../../components/ErrorView';
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

const defaultFilter: FilterValues = {dateFrom: null, dateTo: null, plantId: '', customerId: ''};

export default function ProductSalesScreen() {
  const [result, setResult] = useState<PagedResult<ProductSalesRow> | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [filter, setFilter] = useState<FilterValues>(defaultFilter);
  const [showFilter, setShowFilter] = useState(false);
  const [page, setPage] = useState(1);

  const load = useCallback(async (f: FilterValues, p: number) => {
    setLoading(true);
    setError(null);
    try {
      const d = await getProductSales({
        dateFrom: f.dateFrom?.toISOString().split('T')[0],
        dateTo: f.dateTo?.toISOString().split('T')[0],
        customerId: f.customerId || undefined,
        page: p, pageSize: 20,
      });
      setResult(d);
    } catch {
      setError('Failed to load product sales data.');
    } finally {
      setLoading(false);
    }
  }, []);

  useFocusEffect(useCallback(() => { load(filter, 1); }, [load, filter]));

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
      <ScrollView style={styles.scroll}>
        <View style={styles.tableWrap}>
          <ReportTable columns={COLS} data={result?.items ?? []} keyExtractor={(_, i) => String(i)} />
        </View>
        {(result?.totalPages ?? 0) > 1 && (
          <View style={styles.pagination}>
            <TouchableOpacity disabled={page <= 1} onPress={() => { const p = page - 1; setPage(p); load(filter, p); }} style={[styles.pageBtn, page <= 1 && styles.pageBtnDisabled]}>
              <Icon name="chevron-left" size={20} color={page <= 1 ? Colors.subtle : Colors.blue} />
            </TouchableOpacity>
            <Text style={styles.pageText}>{page} / {result?.totalPages}</Text>
            <TouchableOpacity disabled={page >= (result?.totalPages ?? 1)} onPress={() => { const p = page + 1; setPage(p); load(filter, p); }} style={[styles.pageBtn, page >= (result?.totalPages ?? 1) && styles.pageBtnDisabled]}>
              <Icon name="chevron-right" size={20} color={page >= (result?.totalPages ?? 1) ? Colors.subtle : Colors.blue} />
            </TouchableOpacity>
          </View>
        )}
      </ScrollView>
      <FilterSheet visible={showFilter} values={filter} onApply={v => { setFilter(v); setPage(1); }} onClose={() => setShowFilter(false)} showCustomer />
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  safe: {flex: 1, backgroundColor: Colors.bg},
  toolbar: {flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center', paddingHorizontal: 16, paddingVertical: 10, backgroundColor: Colors.card, borderBottomWidth: 1, borderBottomColor: Colors.border},
  count: {fontSize: 13, color: Colors.subtle},
  filterBtn: {flexDirection: 'row', alignItems: 'center', gap: 4, paddingHorizontal: 12, paddingVertical: 6, backgroundColor: Colors.blueLight, borderRadius: 6},
  filterTxt: {fontSize: 13, color: Colors.blue, fontWeight: '600'},
  scroll: {flex: 1},
  tableWrap: {backgroundColor: Colors.card, margin: 12, borderRadius: 8, overflow: 'hidden', elevation: 1},
  pagination: {flexDirection: 'row', alignItems: 'center', justifyContent: 'center', gap: 16, padding: 16},
  pageBtn: {padding: 4},
  pageBtnDisabled: {opacity: 0.4},
  pageText: {fontSize: 13, color: Colors.text, fontWeight: '600'},
});

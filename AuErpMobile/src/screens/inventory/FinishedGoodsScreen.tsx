import React, {useCallback, useState} from 'react';
import {ScrollView, StyleSheet, Text, TouchableOpacity, View} from 'react-native';
import {SafeAreaView} from 'react-native-safe-area-context';
import Icon from 'react-native-vector-icons/MaterialIcons';
import {useFocusEffect} from '@react-navigation/native';
import {getFinishedGoods, FinishedGoodsDto, FinishedGoodsRow} from '../../api/inventory';
import KpiCard from '../../components/KpiCard';
import ReportTable, {Column} from '../../components/ReportTable';
import FilterSheet, {FilterValues} from '../../components/FilterSheet';
import LoadingView from '../../components/LoadingView';
import ErrorView from '../../components/ErrorView';
import SectionHeader from '../../components/SectionHeader';
import {Colors} from '../../theme/colors';

const fmtPKR = (n: number) => `${(n / 1000).toFixed(0)}K`;

const COLS: Column<FinishedGoodsRow>[] = [
  {key: 'materialDescription', label: 'Material', flex: 2},
  {key: 'grade', label: 'Grade', flex: 0.7, align: 'center'},
  {key: 'batchNumber', label: 'Batch', flex: 1.2},
  {key: 'quantity', label: 'Qty', flex: 0.8, align: 'right'},
  {key: 'unitOfMeasure', label: 'UoM', flex: 0.6, align: 'center'},
  {key: 'stockValue', label: 'Value (K)', flex: 1, align: 'right', render: v => fmtPKR(Number(v))},
];

const defaultFilter: FilterValues = {dateFrom: null, dateTo: null, plantId: '', customerId: ''};

export default function FinishedGoodsScreen() {
  const [data, setData] = useState<FinishedGoodsDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [filter, setFilter] = useState<FilterValues>(defaultFilter);
  const [showFilter, setShowFilter] = useState(false);
  const [page, setPage] = useState(1);

  const load = useCallback(async (f: FilterValues, p: number) => {
    setLoading(true);
    setError(null);
    try {
      const d = await getFinishedGoods({plantId: f.plantId || undefined, page: p, pageSize: 20});
      setData(d);
    } catch {
      setError('Failed to load inventory data.');
    } finally {
      setLoading(false);
    }
  }, []);

  useFocusEffect(useCallback(() => { load(filter, 1); }, [load, filter]));

  if (loading && !data) return <LoadingView />;
  if (error && !data) return <ErrorView message={error} onRetry={() => load(filter, page)} />;

  const kpis = data?.kpis;

  return (
    <SafeAreaView style={styles.safe} edges={['bottom']}>
      <View style={styles.toolbar}>
        <Text style={styles.count}>{data?.rows.totalCount ?? 0} lines</Text>
        <TouchableOpacity style={styles.filterBtn} onPress={() => setShowFilter(true)}>
          <Icon name="filter-list" size={16} color={Colors.blue} />
          <Text style={styles.filterTxt}>Filter</Text>
        </TouchableOpacity>
      </View>
      <ScrollView contentContainerStyle={styles.content}>
        <SectionHeader title="Stock Summary" />
        <View style={styles.kpiRow}>
          <KpiCard label="Total Qty" value={String(kpis?.totalQuantity ?? 0)} accent={Colors.blue} />
          <KpiCard label="Stock Value" value={`PKR ${fmtPKR(kpis?.totalStockValue ?? 0)}`} accent={Colors.green} />
        </View>
        <View style={styles.kpiRow}>
          <KpiCard label="Grade A" value={String(kpis?.gradeAQuantity ?? 0)} accent={Colors.green} />
          <KpiCard label="Grade B" value={String(kpis?.gradeBQuantity ?? 0)} accent={Colors.orange} />
        </View>
        <View style={styles.kpiRow}>
          <KpiCard label="Grade C" value={String(kpis?.gradeCQuantity ?? 0)} accent={Colors.red} />
          <View style={{flex: 1, minWidth: 140}} />
        </View>

        <SectionHeader title="Stock Lines" />
        <View style={styles.tableWrap}>
          <ReportTable columns={COLS} data={data?.rows.items ?? []} keyExtractor={(_, i) => String(i)} />
        </View>

        {(data?.rows.totalPages ?? 0) > 1 && (
          <View style={styles.pagination}>
            <TouchableOpacity disabled={page <= 1} onPress={() => { const p = page - 1; setPage(p); load(filter, p); }} style={[styles.pageBtn, page <= 1 && styles.pageBtnDisabled]}>
              <Icon name="chevron-left" size={20} color={page <= 1 ? Colors.subtle : Colors.blue} />
            </TouchableOpacity>
            <Text style={styles.pageText}>{page} / {data?.rows.totalPages}</Text>
            <TouchableOpacity disabled={page >= (data?.rows.totalPages ?? 1)} onPress={() => { const p = page + 1; setPage(p); load(filter, p); }} style={[styles.pageBtn, page >= (data?.rows.totalPages ?? 1) && styles.pageBtnDisabled]}>
              <Icon name="chevron-right" size={20} color={page >= (data?.rows.totalPages ?? 1) ? Colors.subtle : Colors.blue} />
            </TouchableOpacity>
          </View>
        )}
      </ScrollView>
      <FilterSheet visible={showFilter} values={filter} onApply={v => { setFilter(v); setPage(1); }} onClose={() => setShowFilter(false)} showPlant />
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

import React, {useCallback, useMemo, useState} from 'react';
import {ScrollView, StyleSheet, Text, TouchableOpacity, View} from 'react-native';
import ScreenSafeArea from '../../components/ScreenSafeArea';
import Icon from 'react-native-vector-icons/MaterialIcons';
import {useFocusEffect} from '@react-navigation/native';
import {getFinishedGoods, FinishedGoodsDto, FinishedGoodsRow} from '../../api/inventory';
import KpiGrid from '../../components/KpiGrid';
import ReportTable, {Column} from '../../components/ReportTable';
import ReportToolbar from '../../components/ReportToolbar';
import SearchBar from '../../components/SearchBar';
import FilterSheet from '../../components/FilterSheet';
import LoadingView from '../../components/LoadingView';
import ErrorView from '../../components/ErrorView';
import SectionHeader from '../../components/SectionHeader';
import {defaultFilter, filterToInventoryParams} from '../../utils/reportFilters';
import {filterRowsBySearch} from '../../utils/tableSearch';
import {formatPkr} from '../../utils/currency';
import {Colors} from '../../theme/colors';

const COLS: Column<FinishedGoodsRow>[] = [
  {key: 'materialDescription', label: 'Material', flex: 2},
  {key: 'grade', label: 'Grade', flex: 0.7, align: 'center'},
  {key: 'batchNumber', label: 'Batch', flex: 1.2},
  {key: 'quantity', label: 'Qty', flex: 0.8, align: 'right'},
  {key: 'unitOfMeasure', label: 'UoM', flex: 0.6, align: 'center'},
  {key: 'stockValue', label: 'Value', flex: 1, align: 'right', render: v => formatPkr(Number(v))},
];

const SEARCH_KEYS: (keyof FinishedGoodsRow)[] = ['materialNumber', 'materialDescription', 'batchNumber', 'grade', 'plant'];

export default function FinishedGoodsScreen() {
  const [data, setData] = useState<FinishedGoodsDto | null>(null);
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
      const d = await getFinishedGoods({...filterToInventoryParams(f), page: p, pageSize: 20});
      setData(d);
    } catch {
      setError('Failed to load inventory data.');
    } finally {
      setLoading(false);
    }
  }, []);

  useFocusEffect(useCallback(() => { load(filter, page); }, [load, filter, page]));

  const filteredRows = useMemo(
    () => filterRowsBySearch(data?.rows.items ?? [], search, SEARCH_KEYS),
    [data?.rows.items, search],
  );

  if (loading && !data) return <LoadingView />;
  if (error && !data) return <ErrorView message={error} onRetry={() => load(filter, page)} />;

  const kpis = data?.kpis;

  return (
    <ScreenSafeArea style={styles.safe}>
      <ReportToolbar
        dateFrom={null}
        dateTo={null}
        onFilterPress={() => setShowFilter(true)}
        loading={loading}
        showQuickRanges={false}
        periodLabel="Current stock"
      />
      <ScrollView nestedScrollEnabled contentContainerStyle={styles.content}>
        <SectionHeader title={`Stock summary (${data?.rows.totalCount ?? 0} lines)`} />
        <KpiGrid
          items={[
            {label: 'SKU lines', value: String(kpis?.totalLines ?? 0), sub: 'current', accent: Colors.blue},
            {label: 'Stock value', value: formatPkr(kpis?.totalStockValue ?? 0), sub: 'current', accent: Colors.green},
            {label: 'Zero stock', value: String(kpis?.zeroStockLines ?? 0), sub: 'current', accent: Colors.red},
            {label: 'Page qty', value: String(kpis?.pageQuantity ?? 0), sub: 'this page', accent: Colors.orange},
            {label: 'Grade A', value: String(kpis?.pageGradeAQuantity ?? 0), sub: 'this page', accent: Colors.green},
            {label: 'Grade B', value: String(kpis?.pageGradeBQuantity ?? 0), sub: 'this page', accent: Colors.orange},
            {label: 'Grade C', value: String(kpis?.pageGradeCQuantity ?? 0), sub: 'this page', accent: Colors.red},
          ]}
        />

        <SectionHeader title="Stock Lines" />
        <SearchBar value={search} onChangeText={setSearch} placeholder="Search stock lines…" />
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
      <FilterSheet visible={showFilter} values={filter} onApply={v => { setFilter(v); setPage(1); setSearch(''); }} onClose={() => setShowFilter(false)} showPlant hideDates />
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

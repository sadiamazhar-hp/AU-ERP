import React, {useCallback, useMemo, useState} from 'react';
import {ScrollView, StyleSheet, Text, TouchableOpacity, View} from 'react-native';
import {SafeAreaView} from 'react-native-safe-area-context';
import Icon from 'react-native-vector-icons/MaterialIcons';
import {useFocusEffect} from '@react-navigation/native';
import {getWorkOrders, WorkOrdersDto, WorkOrderRow} from '../../api/production';
import KpiCard from '../../components/KpiCard';
import BarChartWidget from '../../components/BarChartWidget';
import ReportTable, {Column} from '../../components/ReportTable';
import SearchBar from '../../components/SearchBar';
import FilterSheet from '../../components/FilterSheet';
import LoadingView from '../../components/LoadingView';
import ErrorView from '../../components/ErrorView';
import SectionHeader from '../../components/SectionHeader';
import {defaultFilter, filterToProductionParams} from '../../utils/reportFilters';
import {filterRowsBySearch} from '../../utils/tableSearch';
import {Colors} from '../../theme/colors';

const fmtDate = (s: string) => (s ? new Date(s).toLocaleDateString() : '');

const COLS: Column<WorkOrderRow>[] = [
  {key: 'workOrderNumber', label: 'WO #', flex: 1.2},
  {key: 'materialDescription', label: 'Material', flex: 2},
  {key: 'status', label: 'Status', flex: 1},
  {key: 'plannedQuantity', label: 'Plan', flex: 0.7, align: 'right'},
  {key: 'actualQuantity', label: 'Actual', flex: 0.8, align: 'right'},
  {key: 'startDate', label: 'Start', flex: 1, render: v => fmtDate(String(v))},
];

const SEARCH_KEYS: (keyof WorkOrderRow)[] = ['workOrderNumber', 'materialDescription', 'status'];

export default function WorkOrdersScreen() {
  const [data, setData] = useState<WorkOrdersDto | null>(null);
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
      const d = await getWorkOrders({...filterToProductionParams(f), page: p, pageSize: 20});
      setData(d);
    } catch {
      setError('Failed to load work orders.');
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
    <SafeAreaView style={styles.safe} edges={['bottom']}>
      <View style={styles.toolbar}>
        <Text style={styles.count}>{data?.rows.totalCount ?? 0} orders</Text>
        <TouchableOpacity style={styles.filterBtn} onPress={() => setShowFilter(true)}>
          <Icon name="filter-list" size={16} color={Colors.blue} />
          <Text style={styles.filterTxt}>Filter</Text>
        </TouchableOpacity>
      </View>
      <ScrollView nestedScrollEnabled contentContainerStyle={styles.content}>
        <SectionHeader title="Status Summary" />
        <View style={styles.kpiRow}>
          <KpiCard label="Total" value={String(kpis?.total ?? 0)} accent={Colors.blue} />
          <KpiCard label="Completion" value={`${(kpis?.completionPercent ?? 0).toFixed(0)}%`} accent={Colors.green} />
        </View>
        <View style={styles.kpiRow}>
          <KpiCard label="Planned" value={String(kpis?.planned ?? 0)} accent={Colors.subtle} />
          <KpiCard label="Released" value={String(kpis?.released ?? 0)} accent={Colors.blue} />
        </View>
        <View style={styles.kpiRow}>
          <KpiCard label="In Progress" value={String(kpis?.inProgress ?? 0)} accent={Colors.orange} />
          <KpiCard label="Completed" value={String(kpis?.completed ?? 0)} accent={Colors.green} />
        </View>

        <BarChartWidget
          title="By status"
          labels={['Planned', 'Released', 'In prog.', 'Done']}
          data={[kpis?.planned ?? 0, kpis?.released ?? 0, kpis?.inProgress ?? 0, kpis?.completed ?? 0]}
          color={Colors.blue}
        />

        <SectionHeader title="Work Order List" />
        <SearchBar value={search} onChangeText={setSearch} placeholder="Search work orders…" />
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
      <FilterSheet visible={showFilter} values={filter} onApply={v => { setFilter(v); setPage(1); setSearch(''); }} onClose={() => setShowFilter(false)} showPlant />
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

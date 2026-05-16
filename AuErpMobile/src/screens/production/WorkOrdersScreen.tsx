import React, {useCallback, useState} from 'react';
import {ScrollView, StyleSheet, Text, TouchableOpacity, View} from 'react-native';
import {SafeAreaView} from 'react-native-safe-area-context';
import Icon from 'react-native-vector-icons/MaterialIcons';
import {useFocusEffect} from '@react-navigation/native';
import {getWorkOrders, WorkOrdersDto, WorkOrderRow} from '../../api/production';
import KpiCard from '../../components/KpiCard';
import ReportTable, {Column} from '../../components/ReportTable';
import FilterSheet, {FilterValues} from '../../components/FilterSheet';
import LoadingView from '../../components/LoadingView';
import ErrorView from '../../components/ErrorView';
import SectionHeader from '../../components/SectionHeader';
import {Colors} from '../../theme/colors';

const fmtDate = (s: string) => s ? new Date(s).toLocaleDateString() : '';

const STATUS_COLOR: Record<string, string> = {
  Planned: Colors.subtle,
  Released: Colors.blue,
  InProgress: Colors.orange,
  Completed: Colors.green,
};

const COLS: Column<WorkOrderRow>[] = [
  {key: 'workOrderNumber', label: 'WO #', flex: 1.2},
  {key: 'materialDescription', label: 'Material', flex: 2},
  {key: 'status', label: 'Status', flex: 1},
  {key: 'plannedQuantity', label: 'Plan', flex: 0.7, align: 'right'},
  {key: 'actualQuantity', label: 'Actual', flex: 0.8, align: 'right'},
  {key: 'startDate', label: 'Start', flex: 1, render: v => fmtDate(String(v))},
];

const defaultFilter: FilterValues = {dateFrom: null, dateTo: null, plantId: '', customerId: ''};

export default function WorkOrdersScreen() {
  const [data, setData] = useState<WorkOrdersDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [filter, setFilter] = useState<FilterValues>(defaultFilter);
  const [showFilter, setShowFilter] = useState(false);
  const [page, setPage] = useState(1);

  const load = useCallback(async (f: FilterValues, p: number) => {
    setLoading(true);
    setError(null);
    try {
      const d = await getWorkOrders({
        dateFrom: f.dateFrom?.toISOString().split('T')[0],
        dateTo: f.dateTo?.toISOString().split('T')[0],
        plantId: f.plantId || undefined,
        page: p, pageSize: 20,
      });
      setData(d);
    } catch {
      setError('Failed to load work orders.');
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
        <Text style={styles.count}>{data?.rows.totalCount ?? 0} orders</Text>
        <TouchableOpacity style={styles.filterBtn} onPress={() => setShowFilter(true)}>
          <Icon name="filter-list" size={16} color={Colors.blue} />
          <Text style={styles.filterTxt}>Filter</Text>
        </TouchableOpacity>
      </View>
      <ScrollView contentContainerStyle={styles.content}>
        <SectionHeader title="Status Summary" />
        <View style={styles.kpiRow}>
          <KpiCard label="Planned" value={String(kpis?.planned ?? 0)} accent={Colors.subtle} />
          <KpiCard label="Released" value={String(kpis?.released ?? 0)} accent={Colors.blue} />
        </View>
        <View style={styles.kpiRow}>
          <KpiCard label="In Progress" value={String(kpis?.inProgress ?? 0)} accent={Colors.orange} />
          <KpiCard label="Completed" value={String(kpis?.completed ?? 0)} accent={Colors.green} />
        </View>

        <SectionHeader title="Work Order List" />
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

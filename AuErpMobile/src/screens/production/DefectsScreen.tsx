import React, {useCallback, useMemo, useState} from 'react';
import {ScrollView, StyleSheet, Text, TouchableOpacity, View} from 'react-native';
import {SafeAreaView} from 'react-native-safe-area-context';
import Icon from 'react-native-vector-icons/MaterialIcons';
import {useFocusEffect} from '@react-navigation/native';
import {getDefects, DefectReportDto, DefectReportRow} from '../../api/production';
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

const COLS: Column<DefectReportRow>[] = [
  {key: 'batchNumber', label: 'Batch', flex: 1.2},
  {key: 'materialDescription', label: 'Material', flex: 2},
  {key: 'grDate', label: 'Date', flex: 1, render: v => fmtDate(String(v))},
  {key: 'defectQty', label: 'Defect', flex: 0.8, align: 'right'},
  {key: 'wastageQty', label: 'Wastage', flex: 0.9, align: 'right'},
  {key: 'qualityGrade', label: 'Grade', flex: 0.9},
];

const SEARCH_KEYS: (keyof DefectReportRow)[] = ['batchNumber', 'materialDescription', 'qualityGrade'];

export default function DefectsScreen() {
  const [data, setData] = useState<DefectReportDto | null>(null);
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
      const d = await getDefects({...filterToProductionParams(f), page: p, pageSize: 20});
      setData(d);
    } catch {
      setError('Failed to load defect data.');
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
        <Text style={styles.count}>{data?.rows.totalCount ?? 0} records</Text>
        <TouchableOpacity style={styles.filterBtn} onPress={() => setShowFilter(true)}>
          <Icon name="filter-list" size={16} color={Colors.blue} />
          <Text style={styles.filterTxt}>Filter</Text>
        </TouchableOpacity>
      </View>

      <ScrollView contentContainerStyle={styles.content}>
        <SectionHeader title="Defect KPIs" />
        <View style={styles.kpiRow}>
          <KpiCard label="Produced" value={String(kpis?.totalProduced ?? 0)} accent={Colors.blue} />
          <KpiCard label="Scrap %" value={`${(kpis?.scrapPercent ?? 0).toFixed(1)}%`} accent={Colors.red} />
        </View>
        <View style={styles.kpiRow}>
          <KpiCard label="Stage wastage" value={String(kpis?.totalWastageQty ?? 0)} accent={Colors.orange} />
          <KpiCard label="Scrap qty" value={String(kpis?.totalDefectQty ?? 0)} accent={Colors.red} />
        </View>

        <SectionHeader title="Quality mix (totals)" />
        <BarChartWidget
          labels={['1st', '2nd', '3rd', 'Scrap']}
          data={[
            kpis?.totalFirstQuality ?? 0,
            kpis?.totalSecondQuality ?? 0,
            kpis?.totalThirdQuality ?? 0,
            kpis?.totalDefectQty ?? 0,
          ]}
          color={Colors.blue}
          decimalPlaces={0}
        />

        <SectionHeader title="Batch Detail" />
        <SearchBar value={search} onChangeText={setSearch} placeholder="Search batches…" />
        <View style={styles.tableWrap}>
          <ReportTable
            columns={COLS}
            data={filteredRows}
            keyExtractor={(row, i) => `${row.batchNumber}-${i}`}
            emptyMessage={search ? 'No matches on this page' : undefined}
          />
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

      <FilterSheet
        visible={showFilter}
        values={filter}
        onApply={v => { setFilter(v); setPage(1); setSearch(''); }}
        onClose={() => setShowFilter(false)}
        showPlant
      />
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

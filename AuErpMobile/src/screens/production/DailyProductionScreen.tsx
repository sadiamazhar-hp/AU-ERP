import React, {useCallback, useState} from 'react';
import {ScrollView, StyleSheet, Text, TouchableOpacity, View} from 'react-native';
import {SafeAreaView} from 'react-native-safe-area-context';
import Icon from 'react-native-vector-icons/MaterialIcons';
import {useFocusEffect} from '@react-navigation/native';
import {getDailyProduction, DailyProductionRow, PagedResult} from '../../api/production';
import ReportTable, {Column} from '../../components/ReportTable';
import FilterSheet, {FilterValues} from '../../components/FilterSheet';
import LoadingView from '../../components/LoadingView';
import ErrorView from '../../components/ErrorView';
import {Colors} from '../../theme/colors';

const PAGE = 20;
const fmtDate = (s: string) => s ? new Date(s).toLocaleDateString() : '';

const COLS: Column<DailyProductionRow>[] = [
  {key: 'grDate', label: 'Date', flex: 1.2, render: v => fmtDate(String(v))},
  {key: 'materialDescription', label: 'Material', flex: 2},
  {key: 'batchNumber', label: 'Batch', flex: 1.2},
  {key: 'quantityProduced', label: 'Qty', flex: 0.8, align: 'right'},
  {key: 'qualityGrade', label: 'Grade', flex: 0.7, align: 'center'},
];

const defaultFilter: FilterValues = {dateFrom: null, dateTo: null, plantId: '', customerId: ''};

export default function DailyProductionScreen() {
  const [result, setResult] = useState<PagedResult<DailyProductionRow> | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [filter, setFilter] = useState<FilterValues>(defaultFilter);
  const [showFilter, setShowFilter] = useState(false);
  const [page, setPage] = useState(1);

  const load = useCallback(async (f: FilterValues, p: number) => {
    setLoading(true);
    setError(null);
    try {
      const data = await getDailyProduction({
        dateFrom: f.dateFrom?.toISOString().split('T')[0],
        dateTo: f.dateTo?.toISOString().split('T')[0],
        plantId: f.plantId || undefined,
        page: p,
        pageSize: PAGE,
      });
      setResult(data);
    } catch {
      setError('Failed to load production data.');
    } finally {
      setLoading(false);
    }
  }, []);

  useFocusEffect(useCallback(() => { load(filter, 1); }, [load, filter]));

  function applyFilter(v: FilterValues) { setFilter(v); setPage(1); }

  if (loading && !result) return <LoadingView />;
  if (error && !result) return <ErrorView message={error} onRetry={() => load(filter, page)} />;

  return (
    <SafeAreaView style={styles.safe} edges={['bottom']}>
      {/* Toolbar */}
      <View style={styles.toolbar}>
        <Text style={styles.count}>{result?.totalCount ?? 0} records</Text>
        <TouchableOpacity style={styles.filterBtn} onPress={() => setShowFilter(true)}>
          <Icon name="filter-list" size={16} color={Colors.blue} />
          <Text style={styles.filterTxt}>Filter</Text>
        </TouchableOpacity>
      </View>

      <ScrollView style={styles.scroll}>
        <View style={styles.tableWrap}>
          <ReportTable
            columns={COLS}
            data={result?.items ?? []}
            keyExtractor={(_, i) => String(i)}
          />
        </View>

        {/* Pagination */}
        {(result?.totalPages ?? 0) > 1 && (
          <View style={styles.pagination}>
            <TouchableOpacity
              disabled={page <= 1}
              onPress={() => { const p = page - 1; setPage(p); load(filter, p); }}
              style={[styles.pageBtn, page <= 1 && styles.pageBtnDisabled]}>
              <Icon name="chevron-left" size={20} color={page <= 1 ? Colors.subtle : Colors.blue} />
            </TouchableOpacity>
            <Text style={styles.pageText}>{page} / {result?.totalPages}</Text>
            <TouchableOpacity
              disabled={page >= (result?.totalPages ?? 1)}
              onPress={() => { const p = page + 1; setPage(p); load(filter, p); }}
              style={[styles.pageBtn, page >= (result?.totalPages ?? 1) && styles.pageBtnDisabled]}>
              <Icon name="chevron-right" size={20} color={page >= (result?.totalPages ?? 1) ? Colors.subtle : Colors.blue} />
            </TouchableOpacity>
          </View>
        )}
      </ScrollView>

      <FilterSheet
        visible={showFilter}
        values={filter}
        onApply={applyFilter}
        onClose={() => setShowFilter(false)}
        showPlant
      />
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  safe: {flex: 1, backgroundColor: Colors.bg},
  toolbar: {
    flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center',
    paddingHorizontal: 16, paddingVertical: 10, backgroundColor: Colors.card,
    borderBottomWidth: 1, borderBottomColor: Colors.border,
  },
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

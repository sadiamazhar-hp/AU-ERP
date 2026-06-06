import React, {useCallback, useEffect, useState} from 'react';
import {RefreshControl, ScrollView, StyleSheet, Text, TouchableOpacity, View} from 'react-native';
import ScreenSafeArea from '../../components/ScreenSafeArea';
import Icon from 'react-native-vector-icons/MaterialIcons';
import {useFocusEffect} from '@react-navigation/native';
import {getDashboard, DashboardDto} from '../../api/dashboard';
import {getProductionSummary} from '../../api/production';
import {getSalesSummary} from '../../api/sales';
import {useAuth} from '../../auth/AuthContext';
import QuickRangeChips from '../../components/QuickRangeChips';
import KpiGrid from '../../components/KpiGrid';
import LoadingView from '../../components/LoadingView';
import ErrorView from '../../components/ErrorView';
import SectionHeader from '../../components/SectionHeader';
import BarChartWidget from '../../components/BarChartWidget';
import LineChartWidget from '../../components/LineChartWidget';
import {formatNumber, formatPkr, formatPkrAxis} from '../../utils/currency';
import {
  defaultFilterDates,
  ensureDate,
  formatPeriodLabel,
  getPeriodSubtitle,
  isValidDate,
  toIsoDate,
} from '../../utils/dateRanges';
import {Colors} from '../../theme/colors';

function getGreeting() {
  const h = new Date().getHours();
  if (h < 12) return 'Good morning';
  if (h < 17) return 'Good afternoon';
  return 'Good evening';
}

export default function DashboardScreen() {
  const {user, signOut} = useAuth();
  const [dateFrom, setDateFrom] = useState(() => defaultFilterDates().dateFrom);
  const [dateTo, setDateTo] = useState(() => defaultFilterDates().dateTo);
  const [data, setData] = useState<DashboardDto | null>(null);
  const [revenueLabels, setRevenueLabels] = useState<string[]>([]);
  const [revenueData, setRevenueData] = useState<number[]>([]);
  const [prodLabels, setProdLabels] = useState<string[]>([]);
  const [prodData, setProdData] = useState<number[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!isValidDate(dateFrom) || !isValidDate(dateTo)) {
      const fallback = defaultFilterDates();
      setDateFrom(fallback.dateFrom);
      setDateTo(fallback.dateTo);
    }
  }, [dateFrom, dateTo]);

  const load = useCallback(async (from: Date | null | undefined, to: Date | null | undefined) => {
    setLoading(true);
    setError(null);
    const fallback = defaultFilterDates();
    const safeFrom = ensureDate(from, fallback.dateFrom);
    const safeTo = ensureDate(to, fallback.dateTo);
    const params = {dateFrom: toIsoDate(safeFrom), dateTo: toIsoDate(safeTo)};
    try {
      const [d, sales, production] = await Promise.all([
        getDashboard(params),
        getSalesSummary(params),
        getProductionSummary(params),
      ]);
      setData(d);
      const monthly = sales.monthlySeries.slice(-6);
      setRevenueLabels(monthly.map(m => m.month));
      setRevenueData(monthly.map(m => m.revenue));
      const weekly = production.weeklySeries.slice(-6);
      setProdLabels(weekly.map(w => w.weekLabel));
      setProdData(weekly.map(w => w.quantityProduced));
    } catch {
      setError('Failed to load dashboard data.');
    } finally {
      setLoading(false);
    }
  }, []);

  useFocusEffect(useCallback(() => { load(dateFrom, dateTo); }, [load, dateFrom, dateTo]));

  function handleQuickRange(from: Date, to: Date) {
    setDateFrom(ensureDate(from));
    setDateTo(ensureDate(to));
  }

  if (loading && !data) return <LoadingView message="Loading dashboard…" />;
  if (error && !data) return <ErrorView message={error} onRetry={() => load(dateFrom, dateTo)} />;

  const firstName = user?.fullName?.split(' ')[0] ?? 'User';
  const periodLabel = formatPeriodLabel(dateFrom, dateTo);
  const periodSub = getPeriodSubtitle(dateFrom, dateTo);

  return (
    <ScreenSafeArea style={styles.safe} edgePreset="top">
      <View style={styles.header}>
        <View style={styles.headerLeft}>
          <View style={styles.avatarBox}>
            <Text style={styles.avatarText}>{firstName.charAt(0).toUpperCase()}</Text>
          </View>
          <View>
            <Text style={styles.greeting}>{getGreeting()}, {firstName}</Text>
            <Text style={styles.subGreeting}>{user?.department ?? 'Management'}</Text>
          </View>
        </View>
        <TouchableOpacity onPress={signOut} style={styles.signOutBtn} hitSlop={{top: 8, bottom: 8, left: 8, right: 8}}>
          <Icon name="logout" size={18} color="rgba(255,255,255,0.6)" />
        </TouchableOpacity>
      </View>

      <View style={styles.quickTabs}>
        <QuickRangeChips
          dateFrom={dateFrom}
          dateTo={dateTo}
          onChange={handleQuickRange}
          disabled={loading}
        />
      </View>

      <ScrollView
        style={styles.scroll}
        contentContainerStyle={styles.content}
        refreshControl={<RefreshControl refreshing={loading} onRefresh={() => load(dateFrom, dateTo)} tintColor={Colors.blue} />}>

        <View style={styles.summaryCard}>
          <View style={styles.summaryTop}>
            <View style={{flex: 1, marginRight: 12}}>
              <Text style={styles.summaryLabel}>Total Revenue</Text>
              <Text style={styles.summaryRevenue} adjustsFontSizeToFit numberOfLines={1} minimumFontScale={0.7}>
                {formatPkr(data?.totalRevenueThisMonth ?? 0)}
              </Text>
            </View>
            <View style={styles.monthBadge}>
              <Icon name="calendar-today" size={12} color={Colors.blue} style={{marginRight: 4}} />
              <Text style={styles.monthBadgeText}>{periodLabel}</Text>
            </View>
          </View>
          <View style={styles.summaryDivider} />
          <View style={styles.summaryStats}>
            <View style={styles.statItem}>
              <Icon name="receipt-long" size={14} color="rgba(255,255,255,0.6)" />
              <Text style={styles.statValue}>{formatNumber(data?.invoicesThisMonth ?? 0)}</Text>
              <Text style={styles.statLabel}>Invoices</Text>
            </View>
            <View style={styles.statDivider} />
            <View style={styles.statItem}>
              <Icon name="factory" size={14} color="rgba(255,255,255,0.6)" />
              <Text style={styles.statValue}>{formatNumber(data?.activeWorkOrders ?? 0)}</Text>
              <Text style={styles.statLabel}>Active WOs</Text>
            </View>
            <View style={styles.statDivider} />
            <View style={styles.statItem}>
              <Icon name="inventory" size={14} color="rgba(255,255,255,0.6)" />
              <Text style={styles.statValue}>{formatNumber(data?.finishedGoodsLines ?? 0)}</Text>
              <Text style={styles.statLabel}>FG lines</Text>
            </View>
          </View>
        </View>

        <SectionHeader title="Trends" accent={Colors.blue} />
        <LineChartWidget
          title="Revenue trend"
          subtitle={periodSub}
          labels={revenueLabels}
          datasets={[{label: 'Revenue', data: revenueData, color: Colors.blue}]}
          decimalPlaces={0}
          formatYLabel={v => formatPkrAxis(Number(v))}
        />
        <BarChartWidget
          title="Production trend"
          subtitle={periodSub}
          labels={prodLabels}
          data={prodData}
          color={Colors.orangeMid}
          decimalPlaces={0}
        />

        <SectionHeader title="Sales Overview" accent={Colors.blue} />
        <KpiGrid
          items={[
            {label: 'Revenue', value: formatPkr(data?.totalRevenueThisMonth ?? 0), sub: periodSub, accent: Colors.blue, icon: 'trending-up'},
            {label: 'Outstanding', value: formatPkr(data?.outstandingRevenueThisMonth ?? 0), sub: periodSub, accent: Colors.orangeMid, icon: 'schedule'},
            {label: 'Invoices', value: String(data?.invoicesThisMonth ?? 0), sub: periodSub, accent: Colors.green, icon: 'receipt'},
            {label: 'Returns', value: String(data?.salesReturnsThisMonth ?? 0), sub: periodSub, accent: Colors.red, icon: 'assignment-return'},
            {label: 'Collected', value: formatPkr(data?.collectedRevenueThisMonth ?? 0), sub: periodSub, accent: Colors.green, icon: 'payments'},
            {label: 'Pending inv.', value: String(data?.pendingInvoices ?? 0), sub: 'current', accent: Colors.orangeMid, icon: 'pending'},
          ]}
        />

        <SectionHeader title="Manufacturing" accent={Colors.orangeMid} />
        <KpiGrid
          items={[
            {label: 'Production', value: formatNumber(data?.totalProductionQtyThisMonth ?? 0), sub: periodSub, accent: Colors.blue, icon: 'precision-manufacturing'},
            {label: 'Work Orders', value: String(data?.activeWorkOrders ?? 0), sub: 'current', accent: Colors.accent, icon: 'assignment'},
            {label: 'Defect rate', value: `${(data?.defectPercentThisMonth ?? 0).toFixed(1)}%`, sub: periodSub, accent: Colors.red, icon: 'warning'},
            {label: 'Prod orders', value: String(data?.productionOrdersThisMonth ?? 0), sub: periodSub, accent: Colors.orangeMid, icon: 'list-alt'},
          ]}
        />

        <SectionHeader title="Inventory" accent={Colors.green} />
        <KpiGrid
          items={[
            {label: 'Finished Goods', value: formatNumber(data?.finishedGoodsLines ?? 0), sub: 'current', accent: Colors.green, icon: 'inventory-2'},
            {label: 'Stock value', value: formatPkr(data?.totalStockValue ?? 0), sub: 'current', accent: Colors.blue, icon: 'account-balance-wallet'},
          ]}
        />

        <View style={{height: 8}} />
      </ScrollView>
    </ScreenSafeArea>
  );
}

const styles = StyleSheet.create({
  safe: {flex: 1, backgroundColor: Colors.shell},
  header: {
    backgroundColor: Colors.shell,
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    paddingHorizontal: 16,
    paddingBottom: 10,
    paddingTop: 6,
  },
  headerLeft: {flexDirection: 'row', alignItems: 'center', gap: 12},
  avatarBox: {width: 38, height: 38, borderRadius: 12, backgroundColor: Colors.blue, justifyContent: 'center', alignItems: 'center'},
  avatarText: {fontSize: 16, fontWeight: '800', color: '#fff'},
  greeting: {fontSize: 16, fontWeight: '700', color: '#fff'},
  subGreeting: {fontSize: 11, color: 'rgba(255,255,255,0.5)', marginTop: 1},
  signOutBtn: {width: 34, height: 34, borderRadius: 10, backgroundColor: 'rgba(255,255,255,0.08)', justifyContent: 'center', alignItems: 'center'},
  quickTabs: {
    backgroundColor: Colors.shell,
    paddingHorizontal: 16,
    paddingBottom: 12,
    borderBottomWidth: 1,
    borderBottomColor: Colors.shellLight,
  },
  scroll: {flex: 1, backgroundColor: Colors.bg},
  content: {padding: 16, paddingTop: 20},
  summaryCard: {
    backgroundColor: Colors.shell,
    borderRadius: 16,
    padding: 20,
    marginBottom: 24,
    shadowColor: Colors.shadow,
    shadowOpacity: 0.2,
    shadowRadius: 12,
    shadowOffset: {width: 0, height: 6},
    elevation: 6,
    borderWidth: 1,
    borderColor: Colors.shellLight,
  },
  summaryTop: {flexDirection: 'row', justifyContent: 'space-between', alignItems: 'flex-start'},
  summaryLabel: {fontSize: 11, color: 'rgba(255,255,255,0.5)', fontWeight: '700', letterSpacing: 0.8, textTransform: 'uppercase', marginBottom: 4},
  summaryRevenue: {fontSize: 32, fontWeight: '900', color: '#fff', letterSpacing: -1},
  monthBadge: {flexDirection: 'row', alignItems: 'center', backgroundColor: Colors.blueLight, borderRadius: 8, paddingHorizontal: 10, paddingVertical: 5},
  monthBadgeText: {fontSize: 11, color: Colors.blue, fontWeight: '700'},
  summaryDivider: {height: 1, backgroundColor: 'rgba(255,255,255,0.08)', marginVertical: 16},
  summaryStats: {flexDirection: 'row', alignItems: 'center'},
  statItem: {flex: 1, alignItems: 'center', gap: 3},
  statValue: {fontSize: 18, fontWeight: '800', color: '#fff'},
  statLabel: {fontSize: 10, color: 'rgba(255,255,255,0.45)', fontWeight: '600', textTransform: 'uppercase'},
  statDivider: {width: 1, height: 32, backgroundColor: 'rgba(255,255,255,0.1)'},
});

import React, {useCallback, useState} from 'react';
import {RefreshControl, ScrollView, StyleSheet, Text, TouchableOpacity, View} from 'react-native';
import {SafeAreaView} from 'react-native-safe-area-context';
import Icon from 'react-native-vector-icons/MaterialIcons';
import {useFocusEffect} from '@react-navigation/native';
import {getDashboard, DashboardDto} from '../../api/dashboard';
import {useAuth} from '../../auth/AuthContext';
import KpiCard from '../../components/KpiCard';
import LoadingView from '../../components/LoadingView';
import ErrorView from '../../components/ErrorView';
import SectionHeader from '../../components/SectionHeader';
import {Colors} from '../../theme/colors';

function fmtPKR(n: number) {
  if (n >= 1_000_000) return `PKR ${(n / 1_000_000).toFixed(1)}M`;
  if (n >= 1_000) return `PKR ${(n / 1_000).toFixed(0)}K`;
  return `PKR ${n}`;
}

function fmt(n: number) {
  if (n >= 1_000_000) return `${(n / 1_000_000).toFixed(1)}M`;
  if (n >= 1_000) return `${(n / 1_000).toFixed(1)}K`;
  return String(n);
}

function getGreeting() {
  const h = new Date().getHours();
  if (h < 12) return 'Good morning';
  if (h < 17) return 'Good afternoon';
  return 'Good evening';
}

const MONTH = new Date().toLocaleString('default', {month: 'long', year: 'numeric'});

export default function DashboardScreen() {
  const {user, signOut} = useAuth();
  const [data, setData] = useState<DashboardDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const d = await getDashboard();
      setData(d);
    } catch {
      setError('Failed to load dashboard data.');
    } finally {
      setLoading(false);
    }
  }, []);

  useFocusEffect(useCallback(() => { load(); }, [load]));

  if (loading && !data) return <LoadingView message="Loading dashboard…" />;
  if (error && !data) return <ErrorView message={error} onRetry={load} />;

  const firstName = user?.fullName?.split(' ')[0] ?? 'User';

  return (
    <SafeAreaView style={styles.safe} edges={['top']}>
      {/* ── Shell header ── */}
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

      <ScrollView
        style={styles.scroll}
        contentContainerStyle={styles.content}
        refreshControl={<RefreshControl refreshing={loading} onRefresh={load} tintColor={Colors.blue} />}>

        {/* ── Monthly summary banner ── */}
        <View style={styles.summaryCard}>
          <View style={styles.summaryTop}>
            <View>
              <Text style={styles.summaryLabel}>Total Revenue</Text>
              <Text style={styles.summaryRevenue}>{fmtPKR(data?.totalSalesThisMonth ?? 0)}</Text>
            </View>
            <View style={styles.monthBadge}>
              <Icon name="calendar-today" size={12} color={Colors.blue} style={{marginRight: 4}} />
              <Text style={styles.monthBadgeText}>{MONTH}</Text>
            </View>
          </View>
          <View style={styles.summaryDivider} />
          <View style={styles.summaryStats}>
            <View style={styles.statItem}>
              <Icon name="receipt-long" size={14} color="rgba(255,255,255,0.6)" />
              <Text style={styles.statValue}>{data?.totalInvoicesThisMonth ?? 0}</Text>
              <Text style={styles.statLabel}>Invoices</Text>
            </View>
            <View style={styles.statDivider} />
            <View style={styles.statItem}>
              <Icon name="factory" size={14} color="rgba(255,255,255,0.6)" />
              <Text style={styles.statValue}>{data?.activeWorkOrders ?? 0}</Text>
              <Text style={styles.statLabel}>Active WOs</Text>
            </View>
            <View style={styles.statDivider} />
            <View style={styles.statItem}>
              <Icon name="inventory" size={14} color="rgba(255,255,255,0.6)" />
              <Text style={styles.statValue}>{fmt(data?.finishedGoodsStock ?? 0)}</Text>
              <Text style={styles.statLabel}>In Stock</Text>
            </View>
          </View>
        </View>

        {/* ── Sales ── */}
        <SectionHeader title="Sales Overview" accent={Colors.blue} />
        <View style={styles.row}>
          <KpiCard
            label="Revenue"
            value={fmtPKR(data?.totalSalesThisMonth ?? 0)}
            sub="this month"
            accent={Colors.blue}
            icon="trending-up"
          />
          <KpiCard
            label="Outstanding"
            value={fmtPKR(data?.outstandingReceivables ?? 0)}
            sub="receivables"
            accent={Colors.orangeMid}
            icon="schedule"
          />
        </View>
        <View style={styles.row}>
          <KpiCard
            label="Invoices"
            value={String(data?.totalInvoicesThisMonth ?? 0)}
            sub="this month"
            accent={Colors.green}
            icon="receipt"
          />
          <KpiCard
            label="Returns"
            value={String(data?.totalReturnsThisMonth ?? 0)}
            sub="this month"
            accent={Colors.red}
            icon="assignment-return"
          />
        </View>

        {/* ── Manufacturing ── */}
        <SectionHeader title="Manufacturing" accent={Colors.orangeMid} />
        <View style={styles.row}>
          <KpiCard
            label="Production"
            value={fmt(data?.totalProductionThisMonth ?? 0)}
            sub="units this month"
            accent={Colors.blue}
            icon="precision-manufacturing"
          />
          <KpiCard
            label="Work Orders"
            value={String(data?.activeWorkOrders ?? 0)}
            sub="active"
            accent={Colors.accent}
            icon="assignment"
          />
        </View>
        <View style={styles.row}>
          <KpiCard
            label="Defects"
            value={fmt(data?.totalDefectsThisMonth ?? 0)}
            sub="this month"
            accent={Colors.red}
            icon="warning"
          />
          <KpiCard
            label="Raw Consumed"
            value={fmt(data?.rawMaterialConsumptionThisMonth ?? 0)}
            sub="units"
            accent={Colors.orangeMid}
            icon="science"
          />
        </View>

        {/* ── Inventory ── */}
        <SectionHeader title="Inventory" accent={Colors.green} />
        <View style={styles.row}>
          <KpiCard
            label="Finished Goods"
            value={fmt(data?.finishedGoodsStock ?? 0)}
            sub="in stock"
            accent={Colors.green}
            icon="inventory-2"
          />
          <View style={{flex: 1}} />
        </View>

        <View style={{height: 8}} />
      </ScrollView>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  safe: {flex: 1, backgroundColor: Colors.shell},

  // Header
  header: {
    backgroundColor: Colors.shell,
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    paddingHorizontal: 16,
    paddingBottom: 14,
    paddingTop: 6,
  },
  headerLeft: {flexDirection: 'row', alignItems: 'center', gap: 12},
  avatarBox: {
    width: 38, height: 38, borderRadius: 12,
    backgroundColor: Colors.blue,
    justifyContent: 'center', alignItems: 'center',
  },
  avatarText: {fontSize: 16, fontWeight: '800', color: '#fff'},
  greeting: {fontSize: 16, fontWeight: '700', color: '#fff'},
  subGreeting: {fontSize: 11, color: 'rgba(255,255,255,0.5)', marginTop: 1},
  signOutBtn: {
    width: 34, height: 34, borderRadius: 10,
    backgroundColor: 'rgba(255,255,255,0.08)',
    justifyContent: 'center', alignItems: 'center',
  },

  scroll: {flex: 1, backgroundColor: Colors.bg},
  content: {padding: 16, paddingTop: 20},

  // Summary banner
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
  monthBadge: {
    flexDirection: 'row', alignItems: 'center',
    backgroundColor: Colors.blueLight, borderRadius: 8,
    paddingHorizontal: 10, paddingVertical: 5,
  },
  monthBadgeText: {fontSize: 11, color: Colors.blue, fontWeight: '700'},
  summaryDivider: {height: 1, backgroundColor: 'rgba(255,255,255,0.08)', marginVertical: 16},
  summaryStats: {flexDirection: 'row', alignItems: 'center'},
  statItem: {flex: 1, alignItems: 'center', gap: 3},
  statValue: {fontSize: 18, fontWeight: '800', color: '#fff'},
  statLabel: {fontSize: 10, color: 'rgba(255,255,255,0.45)', fontWeight: '600', textTransform: 'uppercase'},
  statDivider: {width: 1, height: 32, backgroundColor: 'rgba(255,255,255,0.1)'},

  row: {flexDirection: 'row', gap: 10, marginBottom: 10},
});

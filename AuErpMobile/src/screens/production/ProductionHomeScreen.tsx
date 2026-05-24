import React from 'react';
import {ScrollView, StyleSheet, Text, View} from 'react-native';
import {SafeAreaView} from 'react-native-safe-area-context';
import {useNavigation} from '@react-navigation/native';
import type {StackNavigationProp} from '@react-navigation/stack';
import Icon from 'react-native-vector-icons/MaterialIcons';
import type {ProductionStackParamList} from '../../navigation/types';
import MenuCard, {MenuGroup} from '../../components/MenuCard';
import {Colors} from '../../theme/colors';

type Nav = StackNavigationProp<ProductionStackParamList, 'ProductionHome'>;

export default function ProductionHomeScreen() {
  const nav = useNavigation<Nav>();
  return (
    <SafeAreaView style={styles.safe} edges={['bottom']}>
      <ScrollView contentContainerStyle={styles.content}>
        {/* Section banner */}
        <View style={styles.banner}>
          <View style={styles.bannerIconWrap}>
            <Icon name="precision-manufacturing" size={28} color="#fff" />
          </View>
          <View style={styles.bannerText}>
            <Text style={styles.bannerTitle}>Manufacturing</Text>
            <Text style={styles.bannerSub}>Production KPIs, batch data & quality</Text>
          </View>
        </View>

        {/* Reports group */}
        <Text style={styles.groupLabel}>Reports</Text>
        <MenuGroup>
          <MenuCard icon="today" label="Daily Production" description="Goods receipt batches per day" color={Colors.blue} onPress={() => nav.navigate('DailyProduction')} />
          <MenuCard icon="bar-chart" label="Production Summary" description="KPIs and weekly output chart" color={Colors.accent} onPress={() => nav.navigate('ProductionSummary')} />
          <MenuCard icon="qr-code-scanner" label="Batch / Lot Tracking" description="Grade breakdown per production lot" color={Colors.cyan} onPress={() => nav.navigate('BatchTracking')} />
        </MenuGroup>

        <Text style={styles.groupLabel}>Quality & Materials</Text>
        <MenuGroup>
          <MenuCard icon="warning" label="Defect & Wastage" description="Quality failure breakdown by batch" color={Colors.red} onPress={() => nav.navigate('Defects')} />
          <MenuCard icon="science" label="Raw Material Consumption" description="Goods issue documents summary" color={Colors.orange} onPress={() => nav.navigate('RawMaterials')} />
          <MenuCard icon="assignment" label="Work Orders" description="Status tracker with KPI counts" color={Colors.green} onPress={() => nav.navigate('WorkOrders')} />
        </MenuGroup>
      </ScrollView>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  safe: {flex: 1, backgroundColor: Colors.bg},
  content: {padding: 16, paddingTop: 20},
  banner: {
    backgroundColor: Colors.shell,
    borderRadius: 14,
    padding: 18,
    flexDirection: 'row',
    alignItems: 'center',
    marginBottom: 24,
    gap: 14,
    shadowColor: Colors.shadow,
    shadowOpacity: 0.15,
    shadowRadius: 8,
    shadowOffset: {width: 0, height: 4},
    elevation: 4,
  },
  bannerIconWrap: {
    width: 52, height: 52, borderRadius: 14,
    backgroundColor: Colors.blue,
    justifyContent: 'center', alignItems: 'center',
  },
  bannerText: {flex: 1},
  bannerTitle: {fontSize: 17, fontWeight: '800', color: '#fff', marginBottom: 3},
  bannerSub: {fontSize: 12, color: 'rgba(255,255,255,0.55)', lineHeight: 17},
  groupLabel: {
    fontSize: 11, fontWeight: '700', color: Colors.subtle,
    textTransform: 'uppercase', letterSpacing: 0.8,
    marginBottom: 8, marginLeft: 2,
  },
});

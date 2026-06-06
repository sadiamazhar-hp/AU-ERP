import React from 'react';
import {ScrollView, StyleSheet, Text, View} from 'react-native';
import ScreenSafeArea from '../../components/ScreenSafeArea';
import {useNavigation} from '@react-navigation/native';
import type {StackNavigationProp} from '@react-navigation/stack';
import Icon from 'react-native-vector-icons/MaterialIcons';
import type {SalesStackParamList} from '../../navigation/types';
import MenuCard, {MenuGroup} from '../../components/MenuCard';
import {Colors} from '../../theme/colors';

type Nav = StackNavigationProp<SalesStackParamList, 'SalesHome'>;

export default function SalesHomeScreen() {
  const nav = useNavigation<Nav>();
  return (
    <ScreenSafeArea style={styles.safe}>
      <ScrollView contentContainerStyle={styles.content}>
        {/* Section banner */}
        <View style={styles.banner}>
          <View style={styles.bannerIconWrap}>
            <Icon name="storefront" size={28} color="#fff" />
          </View>
          <View style={styles.bannerText}>
            <Text style={styles.bannerTitle}>Sales</Text>
            <Text style={styles.bannerSub}>Revenue, invoices, customers & returns</Text>
          </View>
        </View>

        <Text style={styles.groupLabel}>Overview</Text>
        <MenuGroup>
          <MenuCard icon="insights" label="Sales Summary" description="Revenue, collections and outstanding" color={Colors.blue} onPress={() => nav.navigate('SalesSummary')} />
          <MenuCard icon="people" label="Customer-wise Sales" description="Revenue and outstanding per customer" color={Colors.accent} onPress={() => nav.navigate('CustomerSales')} />
          <MenuCard icon="category" label="Product-wise Sales" description="Quantity and revenue per material" color={Colors.cyan} onPress={() => nav.navigate('ProductSales')} />
        </MenuGroup>

        <Text style={styles.groupLabel}>Documents</Text>
        <MenuGroup>
          <MenuCard icon="receipt" label="Invoice Report" description="Invoice list with payment status" color={Colors.green} onPress={() => nav.navigate('Invoices')} />
          <MenuCard icon="assignment-return" label="Sales Returns" description="Return list with credit memo status" color={Colors.red} onPress={() => nav.navigate('SalesReturns')} />
        </MenuGroup>
      </ScrollView>
    </ScreenSafeArea>
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
    backgroundColor: Colors.green,
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

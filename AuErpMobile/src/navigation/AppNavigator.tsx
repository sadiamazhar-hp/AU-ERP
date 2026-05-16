import React from 'react';
import {ActivityIndicator, View} from 'react-native';
import {NavigationContainer} from '@react-navigation/native';
import {createStackNavigator} from '@react-navigation/stack';
import {createBottomTabNavigator} from '@react-navigation/bottom-tabs';
import Icon from 'react-native-vector-icons/MaterialIcons';

import {useAuth} from '../auth/AuthContext';
import {Colors} from '../theme/colors';

import LoginScreen from '../screens/auth/LoginScreen';
import DashboardScreen from '../screens/dashboard/DashboardScreen';

// Production screens
import ProductionHomeScreen from '../screens/production/ProductionHomeScreen';
import DailyProductionScreen from '../screens/production/DailyProductionScreen';
import ProductionSummaryScreen from '../screens/production/ProductionSummaryScreen';
import DefectsScreen from '../screens/production/DefectsScreen';
import RawMaterialsScreen from '../screens/production/RawMaterialsScreen';
import BatchTrackingScreen from '../screens/production/BatchTrackingScreen';
import WorkOrdersScreen from '../screens/production/WorkOrdersScreen';

// Sales screens
import SalesHomeScreen from '../screens/sales/SalesHomeScreen';
import SalesSummaryScreen from '../screens/sales/SalesSummaryScreen';
import InvoicesScreen from '../screens/sales/InvoicesScreen';
import CustomerSalesScreen from '../screens/sales/CustomerSalesScreen';
import ProductSalesScreen from '../screens/sales/ProductSalesScreen';
import SalesReturnsScreen from '../screens/sales/SalesReturnsScreen';

// Inventory screens
import InventoryHomeScreen from '../screens/inventory/InventoryHomeScreen';
import FinishedGoodsScreen from '../screens/inventory/FinishedGoodsScreen';

import type {
  RootStackParamList,
  AuthStackParamList,
  MainTabParamList,
  ProductionStackParamList,
  SalesStackParamList,
  InventoryStackParamList,
} from './types';

const Root = createStackNavigator<RootStackParamList>();
const AuthStack = createStackNavigator<AuthStackParamList>();
const Tab = createBottomTabNavigator<MainTabParamList>();
const ProdStack = createStackNavigator<ProductionStackParamList>();
const SalesStack = createStackNavigator<SalesStackParamList>();
const InvStack = createStackNavigator<InventoryStackParamList>();

const screenOptions = {
  headerStyle: {backgroundColor: Colors.shell},
  headerTintColor: '#ffffff',
  headerTitleStyle: {fontWeight: '600' as const},
};

function ProductionNavigator() {
  return (
    <ProdStack.Navigator screenOptions={screenOptions}>
      <ProdStack.Screen name="ProductionHome" component={ProductionHomeScreen} options={{title: 'Manufacturing'}} />
      <ProdStack.Screen name="DailyProduction" component={DailyProductionScreen} options={{title: 'Daily Production'}} />
      <ProdStack.Screen name="ProductionSummary" component={ProductionSummaryScreen} options={{title: 'Production Summary'}} />
      <ProdStack.Screen name="Defects" component={DefectsScreen} options={{title: 'Defect & Wastage'}} />
      <ProdStack.Screen name="RawMaterials" component={RawMaterialsScreen} options={{title: 'Raw Material Consumption'}} />
      <ProdStack.Screen name="BatchTracking" component={BatchTrackingScreen} options={{title: 'Batch Tracking'}} />
      <ProdStack.Screen name="WorkOrders" component={WorkOrdersScreen} options={{title: 'Work Orders'}} />
    </ProdStack.Navigator>
  );
}

function SalesNavigator() {
  return (
    <SalesStack.Navigator screenOptions={screenOptions}>
      <SalesStack.Screen name="SalesHome" component={SalesHomeScreen} options={{title: 'Sales'}} />
      <SalesStack.Screen name="SalesSummary" component={SalesSummaryScreen} options={{title: 'Sales Summary'}} />
      <SalesStack.Screen name="Invoices" component={InvoicesScreen} options={{title: 'Invoice Report'}} />
      <SalesStack.Screen name="CustomerSales" component={CustomerSalesScreen} options={{title: 'Customer-wise Sales'}} />
      <SalesStack.Screen name="ProductSales" component={ProductSalesScreen} options={{title: 'Product-wise Sales'}} />
      <SalesStack.Screen name="SalesReturns" component={SalesReturnsScreen} options={{title: 'Sales Returns'}} />
    </SalesStack.Navigator>
  );
}

function InventoryNavigator() {
  return (
    <InvStack.Navigator screenOptions={screenOptions}>
      <InvStack.Screen name="InventoryHome" component={InventoryHomeScreen} options={{title: 'Inventory'}} />
      <InvStack.Screen name="FinishedGoods" component={FinishedGoodsScreen} options={{title: 'Finished Goods'}} />
    </InvStack.Navigator>
  );
}

const TAB_ICONS: Record<string, string> = {
  Dashboard: 'dashboard',
  Production: 'precision-manufacturing',
  Sales: 'storefront',
  Inventory: 'inventory',
};

function MainTabs() {
  return (
    <Tab.Navigator
      screenOptions={({route}) => ({
        tabBarIcon: ({color, size, focused}) => (
          <Icon name={TAB_ICONS[route.name] ?? 'circle'} size={focused ? size + 1 : size} color={color} />
        ),
        tabBarActiveTintColor: Colors.blue,
        tabBarInactiveTintColor: Colors.subtle,
        tabBarStyle: {
          backgroundColor: Colors.card,
          borderTopColor: Colors.border,
          borderTopWidth: 1,
          height: 60,
          paddingBottom: 8,
          paddingTop: 6,
          shadowColor: Colors.shadow,
          shadowOpacity: 0.08,
          shadowRadius: 8,
          shadowOffset: {width: 0, height: -2},
          elevation: 8,
        },
        tabBarLabelStyle: {fontSize: 11, fontWeight: '600', marginTop: 2},
        headerShown: false,
      })}>
      <Tab.Screen name="Dashboard" component={DashboardScreen} />
      <Tab.Screen name="Production" component={ProductionNavigator} />
      <Tab.Screen name="Sales" component={SalesNavigator} />
      <Tab.Screen name="Inventory" component={InventoryNavigator} />
    </Tab.Navigator>
  );
}

function AuthNavigator() {
  return (
    <AuthStack.Navigator screenOptions={{headerShown: false}}>
      <AuthStack.Screen name="Login" component={LoginScreen} />
    </AuthStack.Navigator>
  );
}

export default function AppNavigator() {
  const {isLoading, token} = useAuth();

  if (isLoading) {
    return (
      <View style={{flex: 1, justifyContent: 'center', alignItems: 'center', backgroundColor: Colors.bg}}>
        <ActivityIndicator size="large" color={Colors.blue} />
      </View>
    );
  }

  return (
    <NavigationContainer>
      <Root.Navigator screenOptions={{headerShown: false}}>
        {token ? (
          <Root.Screen name="Main" component={MainTabs} />
        ) : (
          <Root.Screen name="Auth" component={AuthNavigator} />
        )}
      </Root.Navigator>
    </NavigationContainer>
  );
}

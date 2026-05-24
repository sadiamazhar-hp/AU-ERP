export type RootStackParamList = {
  Auth: undefined;
  Main: undefined;
};

export type AuthStackParamList = {
  Login: undefined;
};

export type MainTabParamList = {
  Dashboard: undefined;
  Production: undefined;
  Sales: undefined;
  Inventory: undefined;
};

export type ProductionStackParamList = {
  ProductionHome: undefined;
  DailyProduction: undefined;
  ProductionSummary: undefined;
  Defects: undefined;
  RawMaterials: undefined;
  BatchTracking: undefined;
  WorkOrders: undefined;
};

export type SalesStackParamList = {
  SalesHome: undefined;
  SalesSummary: undefined;
  Invoices: undefined;
  CustomerSales: undefined;
  ProductSales: undefined;
  SalesReturns: undefined;
};

export type InventoryStackParamList = {
  InventoryHome: undefined;
  FinishedGoods: undefined;
};

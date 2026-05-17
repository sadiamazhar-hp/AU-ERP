import client from './client';

/** Mirrors backend MobileDashboardDto (api/mobile/dashboard). */
export interface DashboardDto {
  productionOrdersThisMonth: number;
  activeWorkOrders: number;
  totalProductionQtyThisMonth: number;
  defectPercentThisMonth: number;
  totalRevenueThisMonth: number;
  collectedRevenueThisMonth: number;
  outstandingRevenueThisMonth: number;
  invoicesThisMonth: number;
  pendingInvoices: number;
  totalStockValue: number;
  finishedGoodsLines: number;
  salesReturnsThisMonth: number;
}

interface MobileDashboardDto {
  productionOrdersThisMonth: number;
  activeWorkOrders: number;
  totalProductionQtyThisMonth: number;
  defectPercentThisMonth: number;
  totalRevenueThisMonth: number;
  collectedRevenueThisMonth: number;
  outstandingRevenueThisMonth: number;
  invoicesThisMonth: number;
  pendingInvoices: number;
  totalStockValue: number;
  finishedGoodsLines: number;
  salesReturnsThisMonth: number;
}

export async function getDashboard(): Promise<DashboardDto> {
  const res = await client.get<{data: MobileDashboardDto}>('/dashboard');
  const data = res.data.data;
  return {
    productionOrdersThisMonth: data.productionOrdersThisMonth,
    activeWorkOrders: data.activeWorkOrders,
    totalProductionQtyThisMonth: data.totalProductionQtyThisMonth,
    defectPercentThisMonth: data.defectPercentThisMonth,
    totalRevenueThisMonth: data.totalRevenueThisMonth,
    collectedRevenueThisMonth: data.collectedRevenueThisMonth,
    outstandingRevenueThisMonth: data.outstandingRevenueThisMonth,
    invoicesThisMonth: data.invoicesThisMonth,
    pendingInvoices: data.pendingInvoices,
    totalStockValue: data.totalStockValue,
    finishedGoodsLines: data.finishedGoodsLines,
    salesReturnsThisMonth: data.salesReturnsThisMonth,
  };
}

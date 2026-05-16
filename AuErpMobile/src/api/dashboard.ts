import client from './client';

export interface DashboardDto {
  totalProductionThisMonth: number;
  activeWorkOrders: number;
  totalSalesThisMonth: number;
  outstandingReceivables: number;
  totalInvoicesThisMonth: number;
  totalReturnsThisMonth: number;
  finishedGoodsStock: number;
  rawMaterialConsumptionThisMonth: number;
  totalDefectsThisMonth: number;
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
    totalProductionThisMonth: data.totalProductionQtyThisMonth,
    activeWorkOrders: data.activeWorkOrders,
    totalSalesThisMonth: data.totalRevenueThisMonth,
    outstandingReceivables: data.outstandingRevenueThisMonth,
    totalInvoicesThisMonth: data.invoicesThisMonth,
    totalReturnsThisMonth: data.salesReturnsThisMonth,
    finishedGoodsStock: data.finishedGoodsLines,
    rawMaterialConsumptionThisMonth: 0,
    totalDefectsThisMonth: data.defectPercentThisMonth,
  };
}

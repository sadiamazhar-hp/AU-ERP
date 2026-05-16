import client from './client';
import {PagedResult} from './production';

export interface SalesFilter {
  dateFrom?: string;
  dateTo?: string;
  customerId?: string;
  materialNumber?: string;
  page?: number;
  pageSize?: number;
}

export interface SalesSummaryKpis {
  totalRevenue: number;
  totalCollected: number;
  totalOutstanding: number;
  totalOrders: number;
  totalInvoices: number;
}

export interface SalesSummaryChartPoint {
  month: string;
  revenue: number;
  collected: number;
}

export interface SalesSummaryDto {
  kpis: SalesSummaryKpis;
  monthlySeries: SalesSummaryChartPoint[];
}

export interface InvoiceRow {
  invoiceNumber: string;
  documentDate: string;
  customerName: string;
  netAmount: number;
  taxAmount: number;
  totalAmount: number;
  amountPaid: number;
  outstanding: number;
  status: string;
}

export interface CustomerSalesRow {
  customerNumber: string;
  customerName: string;
  totalRevenue: number;
  totalCollected: number;
  totalOutstanding: number;
  invoiceCount: number;
}

export interface ProductSalesRow {
  materialNumber: string;
  materialDescription: string;
  totalQuantity: number;
  unitOfMeasure: string;
  totalRevenue: number;
  invoiceCount: number;
}

export interface SalesReturnKpis {
  totalReturns: number;
  totalReturnValue: number;
  pendingCreditMemos: number;
  approvedCreditMemos: number;
}

export interface SalesReturnRow {
  returnNumber: string;
  returnDate: string;
  customerName: string;
  materialDescription: string;
  returnQuantity: number;
  returnValue: number;
  creditMemoStatus: string;
  qualityStatus: string;
}

export interface SalesReturnsDto {
  kpis: SalesReturnKpis;
  rows: PagedResult<SalesReturnRow>;
}

export async function getSalesSummary(filter: SalesFilter): Promise<SalesSummaryDto> {
  const res = await client.get<{data: any}>('/reports/sales/summary', {params: filter});
  const data = res.data.data;
  return {
    kpis: {
      totalRevenue: data.kpis?.totalRevenue ?? 0,
      totalCollected: data.kpis?.collectedRevenue ?? 0,
      totalOutstanding: data.kpis?.outstandingRevenue ?? 0,
      totalOrders: data.kpis?.totalOrders ?? 0,
      totalInvoices: data.kpis?.totalInvoices ?? 0,
    },
    monthlySeries: (data.chartSeries ?? []).map((row: any) => ({
      month: row.label,
      revenue: row.revenue,
      collected: row.collected,
    })),
  };
}

export async function getInvoices(filter: SalesFilter): Promise<PagedResult<InvoiceRow>> {
  const res = await client.get<{data: any}>('/reports/sales/invoices', {params: filter});
  return mapPagedResult(res.data.data, (row: any) => ({
    invoiceNumber: row.documentNumber,
    documentDate: row.documentDate,
    customerName: row.customerName,
    netAmount: row.subtotal,
    taxAmount: Number(row.grandTotal ?? 0) - Number(row.subtotal ?? 0),
    totalAmount: row.grandTotal,
    amountPaid: row.status === 'Collected' ? row.grandTotal : 0,
    outstanding: row.status === 'Collected' ? 0 : row.grandTotal,
    status: row.status,
  }));
}

export async function getCustomerSales(filter: SalesFilter): Promise<PagedResult<CustomerSalesRow>> {
  const res = await client.get<{data: any}>('/reports/sales/customers', {params: filter});
  return mapPagedResult(res.data.data, (row: any) => ({
    customerNumber: row.customerId ?? '',
    customerName: row.customerName,
    totalRevenue: row.totalAmount,
    totalCollected: row.collectedAmount,
    totalOutstanding: row.outstandingAmount,
    invoiceCount: row.invoiceCount,
  }));
}

export async function getProductSales(filter: SalesFilter): Promise<PagedResult<ProductSalesRow>> {
  const res = await client.get<{data: any}>('/reports/sales/products', {params: filter});
  return mapPagedResult(res.data.data, (row: any) => ({
    materialNumber: row.materialNumber,
    materialDescription: row.materialDescription,
    totalQuantity: row.totalQty,
    unitOfMeasure: row.uom,
    totalRevenue: row.totalRevenue,
    invoiceCount: row.invoiceLineCount,
  }));
}

export async function getSalesReturns(filter: SalesFilter): Promise<SalesReturnsDto> {
  const res = await client.get<{data: any}>('/reports/sales/returns', {params: filter});
  const data = res.data.data;
  return {
    kpis: {
      totalReturns: data.kpis?.totalReturns ?? 0,
      totalReturnValue: data.kpis?.totalReturnValue ?? 0,
      pendingCreditMemos: data.kpis?.pendingQualityInspection ?? 0,
      approvedCreditMemos: data.kpis?.creditMemoIssued ?? 0,
    },
    rows: mapPagedResult(data.rows, (row: any) => ({
      returnNumber: row.documentNumber,
      returnDate: row.documentDate,
      customerName: row.customerName,
      materialDescription: row.invoiceNumber,
      returnQuantity: 0,
      returnValue: row.invoiceTotal,
      creditMemoStatus: row.hasCreditMemo ? 'Issued' : 'Pending',
      qualityStatus: row.hasQualityInspection ? 'Done' : 'Pending',
    })),
  };
}

function mapPagedResult<TIn, TOut>(data: any, mapRow: (row: TIn) => TOut): PagedResult<TOut> {
  const rows = (data?.rows ?? []).map(mapRow);
  const totalCount = data?.total ?? 0;
  const page = data?.page ?? 1;
  const pageSize = data?.pageSize ?? (rows.length || 1);
  return {
    items: rows,
    totalCount,
    page,
    pageSize,
    totalPages: Math.max(1, Math.ceil(totalCount / pageSize)),
  };
}

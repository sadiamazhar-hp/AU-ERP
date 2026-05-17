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
  paidInvoices: number;
  unpaidInvoices: number;
  returnInProcessInvoices: number;
  averageInvoiceValue: number;
  totalReturns: number;
  totalReturnCreditValue: number;
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
  pendingQualityInspection: number;
  creditMemoIssued: number;
  totalCreditIssued: number;
}

export interface SalesReturnRow {
  returnNumber: string;
  returnDate: string;
  customerName: string;
  invoiceNumber: string;
  returnReason: string;
  invoiceTotal: number;
  creditAmount: number | null;
  hasQualityInspection: boolean;
  hasCreditMemo: boolean;
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
      paidInvoices: data.kpis?.paidInvoices ?? 0,
      unpaidInvoices: data.kpis?.unpaidInvoices ?? 0,
      returnInProcessInvoices: data.kpis?.returnInProcessInvoices ?? 0,
      averageInvoiceValue: data.kpis?.averageInvoiceValue ?? 0,
      totalReturns: data.kpis?.totalReturns ?? 0,
      totalReturnCreditValue: data.kpis?.totalReturnCreditValue ?? 0,
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
      pendingQualityInspection: data.kpis?.pendingQualityInspection ?? 0,
      creditMemoIssued: data.kpis?.creditMemoIssued ?? 0,
      totalCreditIssued: data.kpis?.totalCreditIssued ?? 0,
    },
    rows: mapPagedResult(data.rows, (row: any) => ({
      returnNumber: row.documentNumber,
      returnDate: row.documentDate,
      customerName: row.customerName,
      invoiceNumber: row.invoiceNumber,
      returnReason: row.returnReason,
      invoiceTotal: row.invoiceTotal,
      creditAmount: row.creditAmount ?? null,
      hasQualityInspection: row.hasQualityInspection,
      hasCreditMemo: row.hasCreditMemo,
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

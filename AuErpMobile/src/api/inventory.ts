import client from './client';
import {PagedResult} from './production';

export interface ReportFilter {
  plantId?: string;
  page?: number;
  pageSize?: number;
}

export interface FinishedGoodsKpis {
  totalLines: number;
  totalStockValue: number;
  zeroStockLines: number;
  /** Sum of quantities on the current page only */
  pageQuantity: number;
  /** Grade totals from current page only */
  pageGradeAQuantity: number;
  pageGradeBQuantity: number;
  pageGradeCQuantity: number;
}

export interface FinishedGoodsRow {
  materialNumber: string;
  materialDescription: string;
  grade: string;
  batchNumber: string;
  quantity: number;
  unitOfMeasure: string;
  movingAveragePrice: number;
  stockValue: number;
  plant: string;
}

export interface FinishedGoodsDto {
  kpis: FinishedGoodsKpis;
  rows: PagedResult<FinishedGoodsRow>;
}

export async function getFinishedGoods(filter: ReportFilter): Promise<FinishedGoodsDto> {
  const res = await client.get<{data: any}>('/reports/inventory/finished-goods', {params: filter});
  const data = res.data.data;
  const pageRows = data.rows?.rows ?? [];
  return {
    kpis: {
      totalLines: data.kpis?.totalLines ?? 0,
      totalStockValue: data.kpis?.totalStockValue ?? 0,
      zeroStockLines: data.kpis?.zeroStockLines ?? 0,
      pageQuantity: pageRows.reduce((sum: number, row: any) => sum + Number(row.quantity ?? 0), 0),
      pageGradeAQuantity: sumGrade(pageRows, 'A'),
      pageGradeBQuantity: sumGrade(pageRows, 'B'),
      pageGradeCQuantity: sumGrade(pageRows, 'C'),
    },
    rows: mapPagedResult(data.rows, (row: any) => ({
      materialNumber: row.materialNumber,
      materialDescription: row.materialDescription,
      grade: row.grade,
      batchNumber: row.batchOrLot,
      quantity: row.quantity,
      unitOfMeasure: row.uom,
      movingAveragePrice: row.standardCostPerUom,
      stockValue: row.stockValue,
      plant: row.plantName,
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

function sumGrade(rows: any[] | undefined, grade: string): number {
  return (rows ?? [])
    .filter(row => String(row.grade ?? '').toUpperCase() === grade)
    .reduce((sum, row) => sum + Number(row.quantity ?? 0), 0);
}

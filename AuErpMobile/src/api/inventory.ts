import client from './client';
import {PagedResult} from './production';

export interface ReportFilter {
  plantId?: string;
  page?: number;
  pageSize?: number;
}

export interface FinishedGoodsKpis {
  totalLines: number;
  totalQuantity: number;
  totalStockValue: number;
  gradeAQuantity: number;
  gradeBQuantity: number;
  gradeCQuantity: number;
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
  return {
    kpis: {
      totalLines: data.kpis?.totalLines ?? 0,
      totalQuantity: (data.rows?.rows ?? []).reduce((sum: number, row: any) => sum + Number(row.quantity ?? 0), 0),
      totalStockValue: data.kpis?.totalStockValue ?? 0,
      gradeAQuantity: sumGrade(data.rows?.rows, 'A'),
      gradeBQuantity: sumGrade(data.rows?.rows, 'B'),
      gradeCQuantity: sumGrade(data.rows?.rows, 'C'),
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

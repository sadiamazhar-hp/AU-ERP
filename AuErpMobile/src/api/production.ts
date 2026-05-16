import client from './client';

export interface ReportFilter {
  dateFrom?: string;
  dateTo?: string;
  plantId?: string;
  page?: number;
  pageSize?: number;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface DailyProductionRow {
  grDate: string;
  batchNumber: string;
  materialNumber: string;
  materialDescription: string;
  quantityProduced: number;
  unitOfMeasure: string;
  qualityGrade: string;
  plant: string;
}

export interface ProductionSummaryKpis {
  totalBatches: number;
  totalQuantityProduced: number;
  gradeAPercentage: number;
  defectRate: number;
}

export interface ProductionSummaryRow {
  weekLabel: string;
  quantityProduced: number;
}

export interface ProductionSummaryDto {
  kpis: ProductionSummaryKpis;
  weeklySeries: ProductionSummaryRow[];
}

export interface DefectKpis {
  totalBatches: number;
  batchesWithDefects: number;
  totalDefectQty: number;
  totalWastageQty: number;
}

export interface DefectReportRow {
  batchNumber: string;
  materialDescription: string;
  grDate: string;
  quantityProduced: number;
  defectQty: number;
  wastageQty: number;
  qualityGrade: string;
}

export interface DefectReportDto {
  kpis: DefectKpis;
  rows: PagedResult<DefectReportRow>;
}

export interface RawMaterialKpis {
  totalIssueDocuments: number;
  totalMaterialsConsumed: number;
  totalQuantityConsumed: number;
}

export interface RawMaterialConsumptionRow {
  issueDate: string;
  documentNumber: string;
  materialNumber: string;
  materialDescription: string;
  quantityIssued: number;
  unitOfMeasure: string;
}

export interface RawMaterialConsumptionDto {
  kpis: RawMaterialKpis;
  rows: PagedResult<RawMaterialConsumptionRow>;
}

export interface BatchTrackingRow {
  batchNumber: string;
  materialNumber: string;
  materialDescription: string;
  grDate: string;
  quantityProduced: number;
  gradeA: number;
  gradeB: number;
  gradeC: number;
  defects: number;
  wastage: number;
  plant: string;
}

export interface WorkOrderKpis {
  planned: number;
  released: number;
  inProgress: number;
  completed: number;
}

export interface WorkOrderRow {
  workOrderNumber: string;
  materialDescription: string;
  status: string;
  plannedQuantity: number;
  actualQuantity: number;
  startDate: string;
  endDate: string;
}

export interface WorkOrdersDto {
  kpis: WorkOrderKpis;
  rows: PagedResult<WorkOrderRow>;
}

export async function getDailyProduction(filter: ReportFilter): Promise<PagedResult<DailyProductionRow>> {
  const res = await client.get<{data: any}>('/reports/production/daily', {params: filter});
  return mapPagedResult(res.data.data, (row: any) => ({
    grDate: row.productionDate,
    batchNumber: row.batchNo,
    materialNumber: row.materialNumber,
    materialDescription: row.materialDescription,
    quantityProduced: row.producedQty,
    unitOfMeasure: '',
    qualityGrade: row.status,
    plant: '',
  }));
}

export async function getProductionSummary(filter: ReportFilter): Promise<ProductionSummaryDto> {
  const res = await client.get<{data: any}>('/reports/production/summary', {params: filter});
  const data = res.data.data;
  return {
    kpis: {
      totalBatches: data.kpis?.totalOrders ?? 0,
      totalQuantityProduced: data.kpis?.totalProducedQty ?? 0,
      gradeAPercentage: percentage(data.kpis?.totalGoodQty ?? 0, data.kpis?.totalProducedQty ?? 0),
      defectRate: data.kpis?.overallDefectPercent ?? 0,
    },
    weeklySeries: (data.chartSeries ?? []).map((row: any) => ({
      weekLabel: row.label,
      quantityProduced: row.producedQty,
    })),
  };
}

export async function getDefects(filter: ReportFilter): Promise<DefectReportDto> {
  const res = await client.get<{data: any}>('/reports/production/defects', {params: filter});
  const data = res.data.data;
  return {
    kpis: {
      totalBatches: data.rows?.total ?? 0,
      batchesWithDefects: (data.rows?.rows ?? []).filter((row: any) => Number(row.scrapQty ?? 0) > 0).length,
      totalDefectQty: data.kpis?.totalScrap ?? 0,
      totalWastageQty: data.kpis?.totalStageWastage ?? 0,
    },
    rows: mapPagedResult(data.rows, (row: any) => ({
      batchNumber: row.batchNo,
      materialDescription: row.materialDescription,
      grDate: row.batchDate,
      quantityProduced: row.producedQty,
      defectQty: row.scrapQty,
      wastageQty: row.stageWastageQty,
      qualityGrade: `${row.defectPercent}% defect`,
    })),
  };
}

export async function getRawMaterials(filter: ReportFilter): Promise<RawMaterialConsumptionDto> {
  const res = await client.get<{data: any}>('/reports/production/raw-materials', {params: filter});
  const data = res.data.data;
  return {
    kpis: {
      totalIssueDocuments: data.kpis?.totalMaterialsConsumed ?? 0,
      totalMaterialsConsumed: data.kpis?.totalMaterialsConsumed ?? 0,
      totalQuantityConsumed: data.kpis?.totalIssuedLines ?? 0,
    },
    rows: mapPagedResult(data.rows, (row: any) => ({
      issueDate: row.documentDate,
      documentNumber: row.documentNumber,
      materialNumber: row.materialNumber,
      materialDescription: row.materialDescription,
      quantityIssued: row.issuedQty,
      unitOfMeasure: row.uom,
    })),
  };
}

export async function getBatches(filter: ReportFilter): Promise<PagedResult<BatchTrackingRow>> {
  const res = await client.get<{data: any}>('/reports/production/batches', {params: filter});
  return mapPagedResult(res.data.data, (row: any) => ({
    batchNumber: row.batchNo,
    materialNumber: row.materialNumber,
    materialDescription: row.materialDescription,
    grDate: row.batchDate,
    quantityProduced: row.producedQty,
    gradeA: row.firstQualityQty,
    gradeB: row.secondQualityQty,
    gradeC: row.thirdQualityQty,
    defects: row.scrapQty,
    wastage: 0,
    plant: '',
  }));
}

export async function getWorkOrders(filter: ReportFilter): Promise<WorkOrdersDto> {
  const res = await client.get<{data: any}>('/reports/production/work-orders', {params: filter});
  const data = res.data.data;
  return {
    kpis: {
      planned: data.kpis?.planned ?? 0,
      released: data.kpis?.released ?? 0,
      inProgress: data.kpis?.inProgress ?? 0,
      completed: data.kpis?.completed ?? 0,
    },
    rows: mapPagedResult(data.rows, (row: any) => ({
      workOrderNumber: row.productionOrderNumber,
      materialDescription: row.materialDescription,
      status: row.status,
      plannedQuantity: row.targetQuantity,
      actualQuantity: row.stagesDone,
      startDate: row.plannedStartDate,
      endDate: row.plannedEndDate,
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

function percentage(part: number, total: number): number {
  if (!total) {
    return 0;
  }
  return (part / total) * 100;
}

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
  goodQty: number;
  defectiveQty: number;
  defectPercent: number;
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
  defectPercent: number;
}

export interface ProductionSummaryDto {
  kpis: ProductionSummaryKpis;
  weeklySeries: ProductionSummaryRow[];
}

export interface DefectKpis {
  totalProduced: number;
  totalFirstQuality: number;
  totalSecondQuality: number;
  totalThirdQuality: number;
  totalDefectQty: number;
  totalWastageQty: number;
  scrapPercent: number;
  rowCount: number;
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
  totalMaterialsConsumed: number;
  totalIssuedLines: number;
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
  goodPercent: number;
  plant: string;
}

export interface WorkOrderKpis {
  total: number;
  planned: number;
  released: number;
  inProgress: number;
  completed: number;
  completionPercent: number;
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
    goodQty: row.goodQty,
    defectiveQty: row.defectiveQty,
    defectPercent: row.defectPercent,
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
      defectPercent: row.defectPercent ?? 0,
    })),
  };
}

export async function getDefects(filter: ReportFilter): Promise<DefectReportDto> {
  const res = await client.get<{data: any}>('/reports/production/defects', {params: filter});
  const data = res.data.data;
  return {
    kpis: {
      totalProduced: data.kpis?.totalProduced ?? 0,
      totalFirstQuality: data.kpis?.totalFirstQuality ?? 0,
      totalSecondQuality: data.kpis?.totalSecondQuality ?? 0,
      totalThirdQuality: data.kpis?.totalThirdQuality ?? 0,
      totalDefectQty: data.kpis?.totalScrap ?? 0,
      totalWastageQty: data.kpis?.totalStageWastage ?? 0,
      scrapPercent: data.kpis?.scrapPercent ?? 0,
      rowCount: data.rows?.total ?? 0,
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
      totalMaterialsConsumed: data.kpis?.totalMaterialsConsumed ?? 0,
      totalIssuedLines: data.kpis?.totalIssuedLines ?? 0,
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
  return mapPagedResult(res.data.data, (row: any) => {
    const produced = Number(row.producedQty ?? 0);
    const good = Number(row.firstQualityQty ?? 0) + Number(row.secondQualityQty ?? 0) + Number(row.thirdQualityQty ?? 0);
    return {
      batchNumber: row.batchNo,
      materialNumber: row.materialNumber,
      materialDescription: row.materialDescription,
      grDate: row.batchDate,
      quantityProduced: produced,
      gradeA: row.firstQualityQty,
      gradeB: row.secondQualityQty,
      gradeC: row.thirdQualityQty,
      defects: row.scrapQty,
      wastage: 0,
      goodPercent: produced > 0 ? (good / produced) * 100 : 0,
      plant: '',
    };
  });
}

export async function getWorkOrders(filter: ReportFilter): Promise<WorkOrdersDto> {
  const res = await client.get<{data: any}>('/reports/production/work-orders', {params: filter});
  const data = res.data.data;
  const total = data.kpis?.total ?? 0;
  const completed = data.kpis?.completed ?? 0;
  return {
    kpis: {
      total,
      planned: data.kpis?.planned ?? 0,
      released: data.kpis?.released ?? 0,
      inProgress: data.kpis?.inProgress ?? 0,
      completed,
      completionPercent: total > 0 ? (completed / total) * 100 : 0,
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
  if (!total) return 0;
  return (part / total) * 100;
}

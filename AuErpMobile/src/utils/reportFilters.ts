import {FilterValues} from '../components/FilterSheet';
import {defaultFilterDates} from './dateRanges';

const initialDates = defaultFilterDates();

export const defaultFilter: FilterValues = {
  dateFrom: initialDates.dateFrom,
  dateTo: initialDates.dateTo,
  plantId: '',
  customerId: '',
  materialNumber: '',
  plantName: '',
  customerName: '',
  productName: '',
};

export function filterToProductionParams(f: FilterValues, page?: number, pageSize?: number) {
  const userPlantIds = (globalThis as any).__AU_USER_PLANT_IDS__ as string[] | undefined;
  const normalizedPlantIds = Array.isArray(userPlantIds)
    ? userPlantIds.map(p => String(p ?? '').trim()).filter(Boolean)
    : [];
  const isAllPlantsSelection = !f.plantId && normalizedPlantIds.length > 1;

  return {
    dateFrom: f.dateFrom?.toISOString().split('T')[0],
    dateTo: f.dateTo?.toISOString().split('T')[0],
    plantId: f.plantId || undefined,
    plantIds: isAllPlantsSelection ? normalizedPlantIds : undefined,
    page,
    pageSize,
  };
}

export function filterToSalesParams(f: FilterValues, page?: number, pageSize?: number) {
  return {
    dateFrom: f.dateFrom?.toISOString().split('T')[0],
    dateTo: f.dateTo?.toISOString().split('T')[0],
    customerId: f.customerId || undefined,
    materialNumber: f.materialNumber || undefined,
    page,
    pageSize,
  };
}

export function filterToInventoryParams(f: FilterValues, page?: number, pageSize?: number) {
  const userPlantIds = (globalThis as any).__AU_USER_PLANT_IDS__ as string[] | undefined;
  const normalizedPlantIds = Array.isArray(userPlantIds)
    ? userPlantIds.map(p => String(p ?? '').trim()).filter(Boolean)
    : [];
  const isAllPlantsSelection = !f.plantId && normalizedPlantIds.length > 1;

  return {
    plantId: f.plantId || undefined,
    plantIds: isAllPlantsSelection ? normalizedPlantIds : undefined,
    page,
    pageSize,
  };
}

import {FilterValues} from '../components/FilterSheet';

export const defaultFilter: FilterValues = {
  dateFrom: null,
  dateTo: null,
  plantId: '',
  customerId: '',
  materialNumber: '',
  plantName: '',
  customerName: '',
  productName: '',
};

export function filterToProductionParams(f: FilterValues, page?: number, pageSize?: number) {
  return {
    dateFrom: f.dateFrom?.toISOString().split('T')[0],
    dateTo: f.dateTo?.toISOString().split('T')[0],
    plantId: f.plantId || undefined,
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
  return {
    plantId: f.plantId || undefined,
    page,
    pageSize,
  };
}

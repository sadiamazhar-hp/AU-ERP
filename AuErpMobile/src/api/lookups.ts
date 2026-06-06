import client from './client';

export interface LookupItem {
  id: string;
  name: string;
}

export interface ProductLookupItem {
  materialNumber: string;
  description: string;
}

async function unwrapList<T>(path: string, search: string, take: number): Promise<T[]> {
  const res = await client.get<{data: T[]}>(path, {params: {search: search || undefined, take}});
  return res.data.data ?? [];
}

export function getPlants(search = '', take = 50) {
  return unwrapList<LookupItem>('/lookups/plants', search, Math.max(take, 200));
}

export function getCustomers(search = '', take = 50) {
  return unwrapList<LookupItem>('/lookups/customers', search, take);
}

export function getProducts(search = '', take = 50) {
  return unwrapList<ProductLookupItem>('/lookups/products', search, take);
}

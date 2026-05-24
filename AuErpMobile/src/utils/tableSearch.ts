export function filterRowsBySearch<T extends Record<string, unknown>>(
  rows: T[],
  query: string,
  keys: (keyof T)[],
): T[] {
  const q = query.trim().toLowerCase();
  if (!q) return rows;
  return rows.filter(row =>
    keys.some(key => String(row[key] ?? '').toLowerCase().includes(q)),
  );
}

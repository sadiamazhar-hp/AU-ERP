export type QuickRange = 'today' | 'lastWeek' | 'lastMonth' | 'lastYear';

export const QUICK_RANGE_LABELS: Record<QuickRange, string> = {
  today: 'Today',
  lastWeek: 'Last Week',
  lastMonth: 'Last Month',
  lastYear: 'This Year',
};

export const QUICK_RANGES: QuickRange[] = ['today', 'lastWeek', 'lastMonth', 'lastYear'];

export function normalizeDate(d: Date): Date {
  return new Date(d.getFullYear(), d.getMonth(), d.getDate());
}

export function addDays(base: Date, days: number): Date {
  const next = new Date(base);
  next.setDate(next.getDate() + days);
  return normalizeDate(next);
}

export function getQuickRangeDates(range: QuickRange): {from: Date; to: Date} {
  const today = normalizeDate(new Date());
  if (range === 'today') return {from: today, to: today};
  if (range === 'lastWeek') return {from: addDays(today, -6), to: today};
  if (range === 'lastMonth') return {from: addDays(today, -29), to: today};
  return {from: new Date(today.getFullYear(), 0, 1), to: today};
}

export function isSameDate(a: Date | null, b: Date): boolean {
  if (!a) return false;
  return normalizeDate(a).getTime() === normalizeDate(b).getTime();
}

export function matchesQuickRange(
  dateFrom: Date | null,
  dateTo: Date | null,
  range: QuickRange,
): boolean {
  const {from, to} = getQuickRangeDates(range);
  return isSameDate(dateFrom, from) && isSameDate(dateTo, to);
}

export function getActiveQuickRange(
  dateFrom: Date | null,
  dateTo: Date | null,
): QuickRange | null {
  for (const range of QUICK_RANGES) {
    if (matchesQuickRange(dateFrom, dateTo, range)) return range;
  }
  return null;
}

export function isValidDate(d: unknown): d is Date {
  return d instanceof Date && !Number.isNaN(d.getTime());
}

export function ensureDate(d: Date | null | undefined, fallback?: Date): Date {
  if (isValidDate(d)) return normalizeDate(d);
  if (isValidDate(fallback)) return normalizeDate(fallback);
  return normalizeDate(new Date());
}

export function toIsoDate(d: Date | null | undefined): string {
  const date = ensureDate(d);
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
}

export function formatDateShort(d: Date | null): string {
  if (!d) return 'Any date';
  return d.toLocaleDateString('en-GB', {day: '2-digit', month: 'short', year: 'numeric'});
}

export function formatPeriodLabel(dateFrom: Date | null, dateTo: Date | null): string {
  if (!dateFrom && !dateTo) return 'All time';
  if (dateFrom && dateTo) {
    const active = getActiveQuickRange(dateFrom, dateTo);
    if (active) return QUICK_RANGE_LABELS[active];
    return `${formatDateShort(dateFrom)} – ${formatDateShort(dateTo)}`;
  }
  if (dateFrom) return `From ${formatDateShort(dateFrom)}`;
  return `Until ${formatDateShort(dateTo)}`;
}

export function getPeriodSubtitle(dateFrom: Date | null, dateTo: Date | null): string {
  const active = getActiveQuickRange(dateFrom, dateTo);
  if (active === 'today') return 'today';
  if (active === 'lastWeek') return 'last 7 days';
  if (active === 'lastMonth') return 'last 30 days';
  if (active === 'lastYear') return 'this year';
  if (dateFrom && dateTo) return 'selected period';
  return 'all time';
}

export function defaultFilterDates(): {dateFrom: Date; dateTo: Date} {
  const {from, to} = getQuickRangeDates('lastMonth');
  return {dateFrom: from, dateTo: to};
}

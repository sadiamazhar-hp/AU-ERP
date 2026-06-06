export function formatPkr(amount: number): string {
  const value = Number.isFinite(amount) ? amount : 0;
  return `PKR ${value.toLocaleString('en-US', {
    minimumFractionDigits: 0,
    maximumFractionDigits: 2,
  })}`;
}

export function formatPkrAxis(amount: number): string {
  const value = Number.isFinite(amount) ? amount : 0;
  if (value >= 1_000_000) return `PKR ${(value / 1_000_000).toFixed(1)}M`;
  if (value >= 1_000) return `PKR ${Math.round(value / 1_000)}K`;
  return formatPkr(value);
}

export function formatNumber(n: number): string {
  const value = Number.isFinite(n) ? n : 0;
  return Math.round(value).toLocaleString('en-US');
}


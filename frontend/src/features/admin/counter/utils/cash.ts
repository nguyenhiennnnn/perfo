const CASH_DENOMINATIONS = [10_000, 20_000, 50_000, 100_000, 200_000, 500_000];
const MAX_CASH_SUGGESTIONS = 4;

export const getCashSuggestions = (amount: number): number[] => {
  if (amount <= 0) return [];
  const rounded = CASH_DENOMINATIONS.map(
    (unit) => Math.ceil(amount / unit) * unit,
  );
  return [...new Set([amount, ...rounded])]
    .sort((a, b) => a - b)
    .slice(0, MAX_CASH_SUGGESTIONS);
};

export const parseMoneyInput = (value: string): number => {
  const parsed = Number(value);
  return Number.isFinite(parsed) ? Math.max(0, Math.round(parsed)) : 0;
};

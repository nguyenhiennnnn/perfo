const MS_PER_DAY = 86_400_000;

const parse = (value: string) => {
  const [year, month, day] = value.split("-").map(Number);
  return Date.UTC(year, month - 1, day);
};

const format = (ms: number) => new Date(ms).toISOString().slice(0, 10);

export const addDays = (value: string, days: number) => format(parse(value) + days * MS_PER_DAY);

export const listDays = (from: string, count: number) =>
  Array.from({ length: count }, (_, index) => addDays(from, index));

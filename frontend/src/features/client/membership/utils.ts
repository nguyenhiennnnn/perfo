import { TIER_ROLE_LAYOUT, TIER_VISUALS } from "./constants";

export type TierRole = keyof typeof TIER_ROLE_LAYOUT;

export function getVisual(index: number) {
  return TIER_VISUALS[index] ?? TIER_VISUALS[TIER_VISUALS.length - 1];
}

export function getFeaturedIndex(count: number) {
  return Math.floor(count / 2);
}

export function getTierRole(index: number, count: number): TierRole {
  if (index === getFeaturedIndex(count)) return "featured";
  if (index === 0) return "first";
  if (index === count - 1) return "last";
  return "middle";
}

export function getTierGridColumns(count: number) {
  return Array.from({ length: count }, (_, index) => TIER_ROLE_LAYOUT[getTierRole(index, count)].weight).join(" ");
}

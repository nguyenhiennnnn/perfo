import { readFileSync } from "node:fs";
import { fileURLToPath } from "node:url";
import { dirname, join } from "node:path";

const MIN_RATIO = 4.5;
const CSS_PATH = join(dirname(fileURLToPath(import.meta.url)), "..", "src", "index.css");
const PLAIN_SURFACES = ["background", "card", "secondary", "muted"];

const TEXT_CHECKS = {
  light: [
    { text: "accent-text", tint: "accent", alphas: [0.05, 0.1] },
    { text: "warning-text", tint: "warning", alphas: [0.05, 0.1] },
    { text: "primary-text", tint: "primary", alphas: [0.1, 0.15] },
  ],
  dark: [
    { text: "accent-text", tint: "accent", alphas: [0.1, 0.2] },
    { text: "warning-text", tint: "warning", alphas: [0.1, 0.2] },
    { text: "primary-text", tint: "primary", alphas: [0.1, 0.15, 0.3] },
  ],
};

const BEFORE_CHECKS = {
  light: [{ text: "accent" }, { text: "warning" }],
  dark: [{ text: "primary" }],
};

const css = readFileSync(CSS_PATH, "utf8");

const readBlock = (selector) => {
  const start = css.search(new RegExp(`^${selector.replace(".", "\\.")} \\{`, "m"));
  const open = css.indexOf("{", start);
  const close = css.indexOf("\n}", open);
  const vars = {};
  for (const match of css.slice(open, close).matchAll(/--([\w-]+):\s*([^;]+);/g)) {
    vars[match[1]] = match[2].trim();
  }
  return vars;
};

const palettes = { light: readBlock(":root"), dark: readBlock(".dark") };

const resolve = (palette, name, depth = 0) => {
  const value = palette[name];
  if (value === undefined || depth > 8) return undefined;
  const ref = value.match(/^var\(--([\w-]+)\)$/);
  return ref ? resolve(palette, ref[1], depth + 1) : value;
};

const parseOklch = (value) => {
  const match = value.match(/^oklch\(\s*([\d.]+)\s+([\d.]+)\s+([\d.]+)\s*\)$/);
  return match ? match.slice(1).map(Number) : undefined;
};

const clamp01 = (v) => Math.min(1, Math.max(0, v));

const oklchToSrgbEncoded = ([L, C, h]) => {
  const a = C * Math.cos((h * Math.PI) / 180);
  const b = C * Math.sin((h * Math.PI) / 180);
  const l = (L + 0.3963377774 * a + 0.2158037573 * b) ** 3;
  const m = (L - 0.1055613458 * a - 0.0638541728 * b) ** 3;
  const s = (L - 0.0894841775 * a - 1.291485548 * b) ** 3;
  const linear = [
    4.0767416621 * l - 3.3077115913 * m + 0.2309699292 * s,
    -1.2684380046 * l + 2.6097574011 * m - 0.3413193965 * s,
    -0.0041960863 * l - 0.7034186147 * m + 1.707614701 * s,
  ];
  return linear.map((v) => {
    const c = clamp01(v);
    return c <= 0.0031308 ? 12.92 * c : 1.055 * c ** (1 / 2.4) - 0.055;
  });
};

const luminance = (encoded) => {
  const [r, g, b] = encoded.map((c) =>
    c <= 0.04045 ? c / 12.92 : ((c + 0.055) / 1.055) ** 2.4,
  );
  return 0.2126 * r + 0.7152 * g + 0.0722 * b;
};

const contrast = (a, b) => {
  const [hi, lo] = [luminance(a), luminance(b)].sort((x, y) => y - x);
  return (hi + 0.05) / (lo + 0.05);
};

const composite = (fg, bg, alpha) => fg.map((c, i) => alpha * c + (1 - alpha) * bg[i]);

const color = (palette, name) => {
  const parsed = parseOklch(resolve(palette, name) ?? "");
  if (!parsed) throw new Error(`Không đọc được token --${name}`);
  return oklchToSrgbEncoded(parsed);
};

const rows = [];

for (const mode of ["light", "dark"]) {
  const palette = palettes[mode];
  const groups = [
    ["after", TEXT_CHECKS[mode]],
    ["before", BEFORE_CHECKS[mode]],
  ];
  for (const [phase, checks] of groups) {
    for (const { text, tint, alphas = [] } of checks) {
      const fg = color(palette, text);
      for (const surfaceName of PLAIN_SURFACES) {
        const surface = color(palette, surfaceName);
        rows.push({ mode, phase, text, surface: surfaceName, ratio: contrast(fg, surface) });
        for (const alpha of alphas) {
          const tinted = composite(color(palette, tint), surface, alpha);
          rows.push({
            mode,
            phase,
            text,
            surface: `${tint}/${Math.round(alpha * 100)} trên ${surfaceName}`,
            ratio: contrast(fg, tinted),
          });
        }
      }
    }
  }
}

const summary = new Map();
for (const row of rows) {
  const key = `${row.mode}|${row.phase}|${row.text}`;
  const current = summary.get(key);
  if (!current || row.ratio < current.ratio) summary.set(key, row);
}

let failed = false;
for (const [key, row] of summary) {
  const [mode, phase, text] = key.split("|");
  const pass = row.ratio >= MIN_RATIO;
  if (phase === "after" && !pass) failed = true;
  console.log(
    `${phase === "after" ? "SAU  " : "TRƯỚC"} ${mode.padEnd(5)} text-${text.padEnd(13)} thấp nhất ${row.ratio.toFixed(2)}:1 (${row.surface}) ${pass ? "ĐẠT" : "KHÔNG ĐẠT"}`,
  );
}

console.log("");
for (const row of rows.filter((r) => r.phase === "after" && !r.surface.includes("/"))) {
  console.log(`${row.mode.padEnd(5)} text-${row.text.padEnd(13)} trên ${row.surface.padEnd(10)} ${row.ratio.toFixed(2)}:1`);
}

process.exit(failed ? 1 : 0);

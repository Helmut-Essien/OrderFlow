#!/usr/bin/env node
/**
 * Writes sitemap.xml, robots.txt, and `public-origin.generated.ts`.
 * Production absolute URLs come from `ORDERFLOW_SITE_URL` (no trailing slash).
 * Dev uses `environment.ts` `siteUrl` for a local sitemap only — never advertises localhost in robots.
 */
import { mkdirSync, readFileSync, unlinkSync, writeFileSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

const root = join(dirname(fileURLToPath(import.meta.url)), '..');
const isProduction = process.argv.includes('--production');
const generatedPath = join(root, 'src/environments/public-origin.generated.ts');
const publicDir = join(root, 'public');
mkdirSync(publicDir, { recursive: true });

function stripSlash(value) {
  return value.replace(/\/$/, '');
}

function readGeneratedOrigin() {
  try {
    const source = readFileSync(generatedPath, 'utf8');
    return stripSlash(source.match(/GENERATED_PUBLIC_ORIGIN = '([^']*)'/)?.[1] ?? '');
  } catch {
    return '';
  }
}

function writeGeneratedOrigin(origin) {
  writeFileSync(
    generatedPath,
    `/** Written by \`scripts/write-public-seo.mjs\` from \`ORDERFLOW_SITE_URL\`. Empty until a public origin is set. */\nexport const GENERATED_PUBLIC_ORIGIN = '${origin}';\n`
  );
}

function writeRobots(sitemapUrl) {
  const lines = ['User-agent: *', 'Allow: /', 'Disallow: /login', 'Disallow: /app', 'Disallow: /404', ''];
  if (sitemapUrl) {
    lines.push(`Sitemap: ${sitemapUrl}`, '');
  }
  writeFileSync(join(publicDir, 'robots.txt'), lines.join('\n'));
}

function writeSitemap(origin) {
  writeFileSync(
    join(publicDir, 'sitemap.xml'),
    `<?xml version="1.0" encoding="UTF-8"?>
<urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">
  <url>
    <loc>${origin}/</loc>
    <changefreq>weekly</changefreq>
    <priority>1.0</priority>
  </url>
</urlset>
`
  );
}

const sitemapPath = join(publicDir, 'sitemap.xml');

if (isProduction) {
  const fromCi = process.env.ORDERFLOW_SITE_URL
    ? stripSlash(process.env.ORDERFLOW_SITE_URL)
    : '';
  if (fromCi) {
    writeGeneratedOrigin(fromCi);
  }

  const origin = fromCi || readGeneratedOrigin();
  writeRobots(origin ? `${origin}/sitemap.xml` : '');
  if (origin) {
    writeSitemap(origin);
  } else {
    try {
      unlinkSync(sitemapPath);
    } catch {
      // No sitemap until a public origin exists.
    }
  }
  process.exit(0);
}

const envSource = readFileSync(join(root, 'src/environments/environment.ts'), 'utf8');
const devOrigin = stripSlash(envSource.match(/siteUrl:\s*'([^']*)'/)?.[1] ?? '');
writeRobots('');
if (devOrigin) {
  writeSitemap(devOrigin);
} else {
  try {
    unlinkSync(sitemapPath);
  } catch {
    // Dev sitemap is optional.
  }
}

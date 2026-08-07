import { chromium } from 'playwright';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import fs from 'node:fs';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const outDir = path.resolve(__dirname, '../../docs/user-guide/screenshots');
fs.mkdirSync(outDir, { recursive: true });

const BASE = process.env.PGM_WEB_BASE || 'http://localhost:5180';
const USER = process.env.PGM_USER || 'AshtonHsu';
const PASS = process.env.PGM_PASS || 'L@run4340';

const pages = [
  { name: '01-login', path: '/login', beforeLogin: true },
  { name: '02-home', path: '/' },
  { name: '03-roles', path: '/system/roles' },
  { name: '04-users', path: '/system/users' },
  { name: '05-change-password', path: '/account/change-password' },
  { name: '06-param-set', path: '/parameters/param-set' },
  { name: '07-reports', path: '/reports' },
  { name: '08-functions', path: '/system/functions' },
  { name: '09-login-history', path: '/query/login-history' },
];

async function waitReady(page) {
  await page.waitForLoadState('networkidle', { timeout: 30000 }).catch(() => {});
  await page.waitForTimeout(800);
}

async function shot(page, name) {
  const file = path.join(outDir, `${name}.png`);
  await page.screenshot({ path: file, fullPage: true });
  console.log('saved', file);
}

const chromeCandidates = [
  process.env.PLAYWRIGHT_CHROME_PATH,
  `${process.env.LOCALAPPDATA}\\ms-playwright\\chromium-1228\\chrome-win\\chrome.exe`,
  `${process.env.LOCALAPPDATA}\\ms-playwright\\chromium-1228\\chrome-win64\\chrome.exe`,
  `${process.env.ProgramFiles}\\Google\\Chrome\\Application\\chrome.exe`,
  `${process.env['ProgramFiles(x86)']}\\Microsoft\\Edge\\Application\\msedge.exe`,
].filter(Boolean);

const executablePath = chromeCandidates.find((p) => fs.existsSync(p));
if (!executablePath) {
  console.error('No Chrome/Edge found. Set PLAYWRIGHT_CHROME_PATH.');
  process.exit(1);
}
console.log('using browser', executablePath);

const browser = await chromium.launch({ headless: true, executablePath });
const context = await browser.newContext({
  viewport: { width: 1440, height: 900 },
  ignoreHTTPSErrors: true,
});
const page = await context.newPage();

try {
  // Login page
  await page.goto(`${BASE}/login`, { waitUntil: 'domcontentloaded', timeout: 60000 });
  await waitReady(page);
  await shot(page, '01-login');

  await page.fill('#login-userid', USER);
  await page.fill('#login-password', PASS);
  await Promise.all([
    page.waitForURL((url) => !url.pathname.includes('/login'), { timeout: 30000 }),
    page.click('button.login-submit'),
  ]);
  await waitReady(page);
  await shot(page, '02-home');

  for (const item of pages.filter((p) => !p.beforeLogin && p.name !== '02-home')) {
    await page.goto(`${BASE}${item.path}`, { waitUntil: 'domcontentloaded', timeout: 60000 });
    await waitReady(page);

    // RoleFunctionSet：選一個角色再截，才有勾選表內容
    if (item.name === '03-roles') {
      const roleSelect = page.locator('#role-select');
      if (await roleSelect.count()) {
        const options = await roleSelect.locator('option').evaluateAll((opts) =>
          opts.map((o) => ({ value: o.value, label: o.textContent?.trim() || '' }))
        );
        const first = options.find((o) => o.value);
        if (first) {
          await roleSelect.selectOption(first.value);
          await waitReady(page);
        }
      }
    }

    await shot(page, item.name);
  }

  // ParamSet after selecting first category + query if available
  await page.goto(`${BASE}/parameters/param-set`, { waitUntil: 'domcontentloaded' });
  await waitReady(page);
  const category = page.locator('#param-category');
  if (await category.count()) {
    const options = await category.locator('option').allTextContents();
    const firstReal = options.find((t) => t && t.trim() && t !== '請選擇');
    if (firstReal) {
      await category.selectOption({ label: firstReal });
      await page.click('button.param-filter-query');
      await waitReady(page);
      await shot(page, '06b-param-set-queried');
    }
  }

  console.log('done');
} catch (err) {
  console.error('capture failed:', err);
  await shot(page, 'zz-error');
  process.exitCode = 1;
} finally {
  await browser.close();
}

// Takes the pictures used in the walkthroughs, from a running, demo-seeded BannerAI.
//   node demo/take-screenshots.mjs        (web http://localhost:3000)
// Needs playwright-core (it is a dev dependency of the web app) and a Chrome or Edge install.
import { chromium } from 'playwright-core'
import fs from 'node:fs'
import path from 'node:path'
import { fileURLToPath } from 'node:url'

const BASE = process.env.WEB ?? 'http://localhost:3000'
const CHROME = process.env.CHROME ?? 'C:/Program Files/Google/Chrome/Application/chrome.exe'
const OUT = path.join(path.dirname(fileURLToPath(import.meta.url)), '..', 'docs', 'demo', 'images')
fs.mkdirSync(OUT, { recursive: true })

const accounts = {
  admin: ['demo-admin@bannerai.demo', 'Demo-Admin-2026!'],
  owner: ['demo-owner@bannerai.demo', 'Demo-Owner-2026!'],
  seller: ['demo-seller@bannerai.demo', 'Demo-Seller-2026!'],
}

const browser = await chromium.launch({ executablePath: CHROME, headless: true })

async function session(who) {
  const page = await (await browser.newContext({ viewport: { width: 1280, height: 800 }, deviceScaleFactor: 1 })).newPage()
  page.setDefaultTimeout(30000)
  await page.goto(`${BASE}/login`)
  await shot(page, `${who}-00-sign-in`)
  await page.fill('#email', accounts[who][0])
  await page.fill('#password', accounts[who][1])
  await page.click('button[type=submit]')
  await page.waitForURL((u) => !/login/.test(u.pathname))
  return page
}

let count = 0
async function shot(page, name, options = {}) {
  await page.waitForTimeout(700)
  await page.screenshot({ path: path.join(OUT, `${name}.png`), fullPage: options.full ?? false })
  count++
  console.log('  ', name)
}

async function visit(page, url, name, wait, options) {
  await page.goto(`${BASE}${url}`)
  if (wait) await page.waitForSelector(wait)
  await shot(page, name, options)
}

// ----- owner
console.log('owner')
{
  const p = await session('owner')
  await shot(p, 'owner-01-home')
  await visit(p, '/banners', 'owner-02-banners', 'text=Welcome Offer')
  await p.click('li:has-text("Welcome Offer") >> text=Open editor')
  await p.waitForSelector('[data-component-id]')
  await p.getByRole('button', { name: 'Fit' }).click()
  await p.locator('[data-component-id]').nth(1).click()
  await shot(p, 'owner-03-editor')
  await p.goBack()
  await p.waitForSelector('text=Weekend Special')
  await p.locator('li:has-text("Weekend Special")').getByRole('button', { name: /schedule/i }).first().click()
  await shot(p, 'owner-04-schedule')
  await visit(p, '/banners/calendar', 'owner-05-calendar', '[data-testid="calendar-range"]')
  await visit(p, '/approvals', 'owner-06-approvals', 'text=Weekend Special')
  await visit(p, '/ads', 'owner-07-ads', '[data-testid^="ad-row-"]')
  await p.getByRole('button', { name: 'Book an ad' }).click()
  await shot(p, 'owner-08-book-an-ad')
  await visit(p, '/ads/statement', 'owner-09-statement', '[data-testid^="statement-"], [data-testid="statement-empty"]')
  await visit(p, '/team', 'owner-10-team', 'text=Sales executives')
  await visit(p, '/media', 'owner-11-files', '[data-testid="storage-usage"]')
  await visit(p, '/default-board', 'owner-12-default-board', '[data-testid="board-preview"]')
  await visit(p, '/subscription', 'owner-13-subscription', 'text=Silver')
  await visit(p, '/shops', 'owner-14-my-shop', 'text=Sunrise Cafe')
  await visit(p, '/takeover', 'owner-15-takeover', 'text=Shop takeover')
  await visit(p, '/dashboard', 'owner-16-messages', '[data-testid="bell-count"]')
  await p.getByRole('button', { name: /Messages/ }).click()
  await shot(p, 'owner-16-messages')
  await visit(p, '/display', 'owner-17-shop-screen', '[data-testid="ad-layout"]')
}

// ----- executive
console.log('executive')
{
  const p = await session('seller')
  await shot(p, 'seller-01-home')
  await visit(p, '/banners', 'seller-02-banners', 'text=Weekend Special')
  await visit(p, '/ads', 'seller-03-ads', '[data-testid^="ad-row-"]')
  await p.getByRole('button', { name: 'Book an ad' }).click()
  await shot(p, 'seller-04-book-an-ad')
}

// ----- administrator
console.log('administrator')
{
  const p = await session('admin')
  await shot(p, 'admin-01-home')
  await visit(p, '/admin/users', 'admin-02-users', 'text=demo-owner@bannerai.demo')
  await visit(p, '/admin/subscriptions', 'admin-03-subscriptions', 'text=Sunrise Cafe')
  await p.goto(`${BASE}/admin/locations`)
  await p.waitForSelector('[data-testid="country-India"]')
  await p.click('[data-testid="country-India"]')
  await p.waitForSelector('[data-testid="state-Maharashtra"]')
  await p.click('[data-testid="state-Maharashtra"]')
  await p.waitForSelector('[data-testid="city-Mumbai"]')
  await p.click('[data-testid="city-Mumbai"]')
  await p.waitForSelector('[data-testid="group-Andheri"]')
  await shot(p, 'admin-04-places')
  await visit(p, '/admin/subscription-plans', 'admin-05-plans', 'text=Silver')
  await visit(p, '/admin/ad-rates', 'admin-06-ad-rates', 'text=Every shop')
  await p.goto(`${BASE}/ads`)
  await p.waitForFunction(() => [...document.querySelectorAll('#ads-shop option')].some((o) => o.textContent.includes('Sunrise Cafe')))
  const value = await p.evaluate(() => [...document.querySelectorAll('#ads-shop option')].find((o) => o.textContent.includes('Sunrise Cafe')).value)
  await p.selectOption('#ads-shop', value)
  await p.waitForSelector('[data-testid^="ad-row-"]')
  await shot(p, 'admin-07-ads')
  await p.getByRole('button', { name: 'Book for a whole place' }).click()
  await shot(p, 'admin-08-book-for-a-place')
  await visit(p, '/ads/statement', 'admin-09-statement', '[data-testid^="statement-"], [data-testid="statement-empty"]')
  await visit(p, '/admin/ad-reports', 'admin-10-ad-report', 'text=Ad report')
  await visit(p, '/takeover', 'admin-11-takeover', 'text=Shop takeover')
  await visit(p, '/admin/audit-log', 'admin-12-activity', 'text=Activity log')
}

await browser.close()
console.log(`\n${count} pictures in ${OUT}`)

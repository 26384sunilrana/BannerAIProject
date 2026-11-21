// Takes the pictures used in the walkthroughs, from a running, demo-seeded BannerAI, and records what worked.
//   node demo/take-screenshots.mjs        (web http://localhost:3000)
// Needs playwright-core (npm install in this folder) and a Chrome or Edge install.
// A step that fails is recorded and the run goes on. The result of every step is written to demo/output/results.json,
// which build-docx.mjs turns into the summary at the top of each Word file. The exit code is 1 when any step failed.
import { chromium } from 'playwright-core'
import fs from 'node:fs'
import path from 'node:path'
import { fileURLToPath } from 'node:url'

const here = path.dirname(fileURLToPath(import.meta.url))
const BASE = process.env.WEB ?? 'http://localhost:3000'
const CHROME = process.env.CHROME ?? 'C:/Program Files/Google/Chrome/Application/chrome.exe'
const OUT = path.join(here, '..', 'docs', 'guides', 'walkthroughs', 'images')
const RESULTS = path.join(here, 'output', 'results.json')
fs.mkdirSync(OUT, { recursive: true })
fs.mkdirSync(path.dirname(RESULTS), { recursive: true })

const accounts = {
  admin: ['demo-admin@bannerai.demo', 'Demo-Admin-2026!'],
  owner: ['demo-owner@bannerai.demo', 'Demo-Owner-2026!'],
  seller: ['demo-seller@bannerai.demo', 'Demo-Seller-2026!'],
}

const browser = await chromium.launch({ executablePath: CHROME, headless: true })
const startedAt = new Date()
const results = []

async function shot(page, name, options = {}) {
  await page.waitForTimeout(700)
  await page.screenshot({ path: path.join(OUT, `${name}.png`), fullPage: options.full ?? false })
}

// Runs one step. Its picture is removed first, so an old picture is never mistaken for a new one.
async function step(role, name, title, action) {
  const file = path.join(OUT, `${name}.png`)
  fs.rmSync(file, { force: true })
  const began = Date.now()
  try {
    await action()
    if (!fs.existsSync(file)) throw new Error('the step finished but took no picture')
    results.push({ role, name, title, ok: true, seconds: (Date.now() - began) / 1000 })
    console.log('  ok    ', name)
  } catch (error) {
    results.push({ role, name, title, ok: false, error: String(error.message ?? error).split('\n')[0], seconds: (Date.now() - began) / 1000 })
    console.log('  FAILED', name, '-', String(error.message ?? error).split('\n')[0])
  }
}

async function visit(page, url, name, wait, options) {
  // A safety net: if what the page should show does not appear (a slow start, a refused call), wait a minute and load it once more.
  for (let attempt = 1; ; attempt++) {
    await page.goto(`${BASE}${url}`)
    if (!wait) break
    try {
      await page.waitForSelector(wait, { timeout: attempt === 1 ? 15000 : 30000 })
      break
    } catch (error) {
      if (attempt >= 2) throw error
      console.log(`         ${name}: not shown, waiting a minute and loading it again`)
      await page.waitForTimeout(65000)
    }
  }
  await shot(page, name, options)
}

async function session(role) {
  const context = await browser.newContext({ viewport: { width: 1280, height: 800 }, deviceScaleFactor: 1 })
  const page = await context.newPage()
  page.setDefaultTimeout(30000)
  await step(role, `${role}-00-sign-in`, 'Sign-in page', async () => {
    await page.goto(`${BASE}/login`)
    await shot(page, `${role}-00-sign-in`)
  })
  await step(role, `${role}-signed-in`, 'Sign in', async () => {
    // Sign-in is limited to 20 a minute from one address (opening pages is not counted). If a run follows another one closely and the sign-in is
    // refused, wait out the minute and try once more. A safety net: a normal run never needs it.
    for (let attempt = 1; ; attempt++) {
      await page.goto(`${BASE}/login`)
      await page.fill('#email', accounts[role][0])
      await page.fill('#password', accounts[role][1])
      await page.click('button[type=submit]')
      try {
        await page.waitForURL((u) => !/login/.test(u.pathname), { timeout: 15000 })
        break
      } catch (error) {
        if (attempt >= 2) throw error
        console.log('         sign-in refused, waiting a minute (rate limit) and trying again')
        await page.waitForTimeout(65000)
      }
    }
    await shot(page, `${role}-signed-in`)
  })
  return page
}

// ----- owner
console.log('owner')
{
  const p = await session('owner')
  const r = 'owner'
  await step(r, 'owner-01-home', 'Home page', () => shot(p, 'owner-01-home'))
  await step(r, 'owner-02-banners', 'Banners list', () => visit(p, '/banners', 'owner-02-banners', 'text=Welcome Offer'))
  await step(r, 'owner-03-editor', 'Banner editor', async () => {
    await p.click('li:has-text("Welcome Offer") >> text=Open editor')
    await p.waitForSelector('[data-component-id]')
    await p.getByRole('button', { name: 'Fit' }).click()
    await p.locator('[data-component-id]').nth(1).click()
    await shot(p, 'owner-03-editor')
  })
  await step(r, 'owner-04-schedule', 'Schedule a banner', async () => {
    await p.goto(`${BASE}/banners`)
    await p.waitForSelector('text=Weekend Special')
    await p.locator('li:has-text("Weekend Special")').getByRole('button', { name: /schedule/i }).first().click()
    await shot(p, 'owner-04-schedule')
  })
  await step(r, 'owner-05-calendar', 'Calendar', () => visit(p, '/banners/calendar', 'owner-05-calendar', '[data-testid="calendar-range"]'))
  await step(r, 'owner-06-approvals', 'Approvals', () => visit(p, '/approvals', 'owner-06-approvals', 'text=Weekend Special'))
  await step(r, 'owner-07-ads', 'Ads list', () => visit(p, '/ads', 'owner-07-ads', '[data-testid^="ad-row-"]'))
  await step(r, 'owner-08-book-an-ad', 'Book an ad', async () => {
    await p.getByRole('button', { name: 'Book an ad' }).click()
    await shot(p, 'owner-08-book-an-ad')
  })
  await step(r, 'owner-09-statement', 'Monthly statement', () => visit(p, '/ads/statement', 'owner-09-statement', '[data-testid^="statement-"], [data-testid="statement-empty"]'))
  await step(r, 'owner-10-team', 'Team', () => visit(p, '/team', 'owner-10-team', 'text=Sales executives'))
  await step(r, 'owner-11-files', 'My files', () => visit(p, '/media', 'owner-11-files', '[data-testid="storage-usage"]'))
  await step(r, 'owner-12-default-board', 'Default board', () => visit(p, '/default-board', 'owner-12-default-board', '[data-testid="board-preview"]'))
  await step(r, 'owner-13-subscription', 'Subscription', () => visit(p, '/subscription', 'owner-13-subscription', 'text=Silver'))
  await step(r, 'owner-14-my-shop', 'My shop', () => visit(p, '/shops', 'owner-14-my-shop', 'text=Sunrise Cafe'))
  await step(r, 'owner-15-takeover', 'Takeover', () => visit(p, '/takeover', 'owner-15-takeover', 'text=Shop takeover'))
  await step(r, 'owner-16-messages', 'Messages', async () => {
    await p.goto(`${BASE}/dashboard`)
    await p.waitForSelector('[data-testid="bell-count"]')
    await p.getByRole('button', { name: /Messages/ }).click()
    await shot(p, 'owner-16-messages')
  })
  await step(r, 'owner-17-shop-screen', 'Shop screen', () => visit(p, '/display', 'owner-17-shop-screen', '[data-testid="ad-layout"]'))
  await step(r, 'owner-18-screens', 'Screens', () => visit(p, '/screens', 'owner-18-screens', 'text=What was shown'))
  await step(r, 'owner-19-account', 'My account and two-step sign-in', async () => {
    await p.goto(`${BASE}/account`)
    await p.waitForSelector('[data-testid="two-factor-status"]')
    await p.locator('#two-factor').scrollIntoViewIfNeeded()
    await shot(p, 'owner-19-account')
  })
}

// ----- executive
console.log('executive')
{
  const p = await session('seller')
  const r = 'seller'
  await step(r, 'seller-01-home', 'Home page', () => shot(p, 'seller-01-home'))
  await step(r, 'seller-02-banners', 'Banners list', () => visit(p, '/banners', 'seller-02-banners', 'text=Weekend Special'))
  await step(r, 'seller-03-ads', 'Ads list', () => visit(p, '/ads', 'seller-03-ads', '[data-testid^="ad-row-"]'))
  await step(r, 'seller-04-book-an-ad', 'Book an ad', async () => {
    await p.getByRole('button', { name: 'Book an ad' }).click()
    await shot(p, 'seller-04-book-an-ad')
  })
  await step(r, 'seller-05-calendar', 'Calendar', () => visit(p, '/banners/calendar', 'seller-05-calendar', '[data-testid="calendar-range"]'))
  await step(r, 'seller-06-files', 'My files', () => visit(p, '/media', 'seller-06-files', '[data-testid="storage-usage"]'))
  await step(r, 'seller-07-screens', 'Screens', () => visit(p, '/screens', 'seller-07-screens', 'text=What was shown'))
  await step(r, 'seller-08-account', 'My account', async () => {
    await p.goto(`${BASE}/account`)
    await p.waitForSelector('[data-testid="two-factor-status"]')
    await shot(p, 'seller-08-account')
  })
}

// ----- administrator
console.log('administrator')
{
  const p = await session('admin')
  const r = 'admin'
  await step(r, 'admin-01-home', 'Home page', () => shot(p, 'admin-01-home'))
  // the lists are filtered to the demo, so that nothing else on the database (other logins, shops made by the end-to-end story) is in the pictures
  await step(r, 'admin-02-users', 'Users', async () => {
    await p.goto(`${BASE}/admin/users`)
    await p.fill('#user-search', 'demo-')
    await p.waitForSelector('text=demo-owner@bannerai.demo')
    await p.waitForFunction(() => !document.body.innerText.includes('exec-') && !document.body.innerText.includes('@gmail.com'))
    await shot(p, 'admin-02-users')
  })
  await step(r, 'admin-03-subscriptions', 'Subscriptions', async () => {
    await p.goto(`${BASE}/admin/subscriptions`)
    await p.fill('input[placeholder="Shop name"]', 'Sunrise')
    await p.waitForSelector('text=Sunrise Cafe')
    await p.waitForFunction(() => !document.body.innerText.includes('Harbour Bakery'))
    await shot(p, 'admin-03-subscriptions')
  })
  await step(r, 'admin-04-places', 'Places', async () => {
    await p.goto(`${BASE}/admin/locations`)
    for (const id of ['country-India', 'state-Maharashtra', 'city-Mumbai']) {
      await p.waitForSelector(`[data-testid="${id}"]`)
      await p.click(`[data-testid="${id}"]`)
    }
    await p.waitForSelector('[data-testid="group-Andheri"]')
    await shot(p, 'admin-04-places')
  })
  await step(r, 'admin-05-plans', 'Subscription plans', () => visit(p, '/admin/subscription-plans', 'admin-05-plans', 'text=Silver'))
  await step(r, 'admin-06-ad-rates', 'Ad rates', () => visit(p, '/admin/ad-rates', 'admin-06-ad-rates', 'text=Every shop'))
  await step(r, 'admin-07-ads', 'Ads of a shop', async () => {
    await p.goto(`${BASE}/ads`)
    await p.waitForFunction(() => [...document.querySelectorAll('#ads-shop option')].some((o) => o.textContent.includes('Sunrise Cafe')))
    const value = await p.evaluate(() => [...document.querySelectorAll('#ads-shop option')].find((o) => o.textContent.includes('Sunrise Cafe')).value)
    await p.selectOption('#ads-shop', value)
    await p.waitForSelector('[data-testid^="ad-row-"]')
    await shot(p, 'admin-07-ads')
  })
  await step(r, 'admin-08-book-for-a-place', 'Book for a whole place', async () => {
    await p.getByRole('button', { name: 'Book for a whole place' }).click()
    await shot(p, 'admin-08-book-for-a-place')
  })
  await step(r, 'admin-09-statement', 'Monthly statement', () => visit(p, '/ads/statement', 'admin-09-statement', '[data-testid^="statement-"], [data-testid="statement-empty"]'))
  await step(r, 'admin-10-ad-report', 'Ad report', () => visit(p, '/admin/ad-reports', 'admin-10-ad-report', 'text=Ad report'))
  await step(r, 'admin-11-takeover', 'Takeover', () => visit(p, '/takeover', 'admin-11-takeover', 'text=Shop takeover'))
  await step(r, 'admin-12-activity', 'Activity log', () => visit(p, '/admin/audit-log', 'admin-12-activity', 'text=Activity log'))
  // six shops to a page, in name order, and no search: page forward until the demo cafe is on screen
  await step(r, 'admin-13-shops', 'Shops', async () => {
    await p.goto(`${BASE}/shops`)
    await p.waitForSelector('text=Total Shops')
    await p.getByRole('link', { name: 'View' }).first().waitFor()
    for (let pages = 0; pages < 20 && !(await p.getByText('Sunrise Cafe', { exact: true }).count()); pages++) {
      await p.getByRole('button', { name: 'Next' }).click()
      await p.waitForTimeout(800)
    }
    await p.getByText('Sunrise Cafe', { exact: true }).waitFor()
    await shot(p, 'admin-13-shops')
  })
}

await browser.close()

const failed = results.filter((x) => !x.ok)
fs.writeFileSync(RESULTS, JSON.stringify({ startedAt: startedAt.toISOString(), finishedAt: new Date().toISOString(), base: BASE, results }, null, 2))
console.log(`\n${results.length - failed.length} of ${results.length} steps worked${failed.length ? `, ${failed.length} FAILED` : ''}. Pictures in ${OUT}`)
process.exit(failed.length ? 1 : 0)

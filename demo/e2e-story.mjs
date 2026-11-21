// One story, start to finish, with every role: the administrator prepares the platform, a new shop owner signs up and subscribes, adds a
// sales executive, the executive makes banners and books an ad, the owner approves, sends one back, the administrator books an ad of their
// own, and the shop screen shows the result. Every step takes a picture and is checked; the result is one Word file.
//
//   node demo/e2e-story.mjs        (API http://localhost:5000, web http://localhost:3000)
//
// Needs a running BannerAI, an administrator login (see start-here.md) and `npm install` in this folder. It makes a NEW shop, owner and
// executive on every run (names end in a short run id), so it can be run again and again on the same database.
// Output: docs/guides/walkthroughs/docx/BannerAI-End-to-End-Demo.docx, and the pictures and results.json in demo/output/e2e/<run id>/.
import { chromium } from 'playwright-core'
import {
  AlignmentType, BorderStyle, Document, HeadingLevel, ImageRun, Packer, PageBreak, Paragraph, ShadingType, Table, TableCell, TableRow, TextRun, WidthType,
} from 'docx'
import crypto from 'node:crypto'
import fs from 'node:fs'
import path from 'node:path'
import { fileURLToPath } from 'node:url'

const here = path.dirname(fileURLToPath(import.meta.url))
const WEB = process.env.WEB ?? 'http://localhost:3000'
const API = (process.env.API ?? 'http://localhost:5000') + '/api'
const CHROME = process.env.CHROME ?? 'C:/Program Files/Google/Chrome/Application/chrome.exe'

const RUN = Date.now().toString(36).slice(-5)
const OUT = path.join(here, 'output', 'e2e', RUN)
const DOCX = path.join(here, '..', 'docs', 'guides', 'walkthroughs', 'docx', 'BannerAI-End-to-End-Demo.docx')
fs.mkdirSync(OUT, { recursive: true })
fs.mkdirSync(path.dirname(DOCX), { recursive: true })

const people = {
  admin: { label: 'Administrator', name: 'Platform administrator', email: process.env.ADMIN_EMAIL ?? 'demo-admin@bannerai.demo', password: process.env.ADMIN_PASSWORD ?? 'Demo-Admin-2026!' },
  owner: { label: 'Shop owner', name: 'Priya Patel', first: 'Priya', last: 'Patel', email: `owner-${RUN}@bannerai.demo`, password: 'E2e-Owner-2026!' },
  exec: { label: 'Sales executive', name: 'Ravi Rao', first: 'Ravi', last: 'Rao', email: `exec-${RUN}@bannerai.demo`, password: 'E2e-Exec-2026!' },
  screen: { label: 'Shop television', name: 'the screen in the shop' }, // nobody signs in on it
}
const SHOP = `Harbour Bakery ${RUN}`
const CITY = `Demo City ${RUN}`
const GROUP = `Old Town ${RUN}`
const BANNER_1 = `Morning Special ${RUN}`
const BANNER_2 = `Festival Offer ${RUN}`
const AD_SHOP = `Fresh Milk Dairy ${RUN}`
const AD_ADMIN = `Union Bank Savings ${RUN}`

// ----- the API, for set-up and for checking (never to do what a person would do on a screen)
async function call(token, method, route, body) {
  const send = () => fetch(API + route, {
    method,
    headers: { 'Content-Type': 'application/json', 'X-Requested-With': 'XMLHttpRequest', ...(token ? { Authorization: `Bearer ${token}` } : {}) },
    body: body === undefined ? undefined : JSON.stringify(body),
  })
  let response = await send()
  // 20 sign-in and "who am I" calls a minute are allowed from one address: when refused, wait the minute out and ask again (once)
  if (response.status === 429) {
    console.log(`         ${method} ${route}: refused by the rate limit, waiting a minute`)
    await new Promise((resolve) => setTimeout(resolve, 65000))
    response = await send()
  }
  const text = await response.text()
  let data = null
  try { data = text ? JSON.parse(text) : null } catch { data = text }
  if (!response.ok) throw new Error(`${method} ${route} -> ${response.status} ${typeof data === 'string' ? data : JSON.stringify(data)}`)
  return data && typeof data === 'object' && 'data' in data && Object.keys(data).every((k) => ['success', 'data', 'message'].includes(k)) ? data.data : data
}
const apiLogin = async (who) => (await call(null, 'POST', '/authentication/login', { email: who.email, password: who.password })).tokens.accessToken
const list = (x) => (Array.isArray(x) ? x : x?.items ?? [])

// ----- time, on the computer's clock (the shop has no time zone set, so the screens use this clock too)
const pad = (n) => String(n).padStart(2, '0')
const local = (d) => `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`
const inDays = (days, hours = 0) => local(new Date(Date.now() + days * 86400000 + hours * 3600000))

// ----- the browser: one window per person
const browser = await chromium.launch({ executablePath: CHROME, headless: true })
const windows = {}
async function windowFor(role) {
  if (!windows[role]) {
    const context = await browser.newContext({ viewport: { width: 1280, height: 800 }, deviceScaleFactor: 1 })
    const page = await context.newPage()
    page.setDefaultTimeout(20000)
    windows[role] = page
  }
  return windows[role]
}

// ----- steps
const results = []
let chapter = ''
let blockedBy = null
let counter = 0

const chapterOf = (title) => { chapter = title }

/**
 * One step of the story.
 *   role    who does it            title   a short name            does   what the person does (plain words)
 *   expect  what must be true afterwards (and is checked)
 *   blocks  when it fails, the steps after it cannot make sense: they are skipped (default true)
 */
async function step({ role, title, does, expect, blocks = true }, action) {
  const n = ++counter
  const id = `${pad(n)}-${role}`
  const record = { n, chapter, role, title, does, expect, shots: [], ok: false, skipped: false, error: null, seconds: 0 }
  results.push(record)
  if (blockedBy) {
    record.skipped = true
    record.error = `Skipped because step ${blockedBy.n} (${blockedBy.title}) failed.`
    console.log(`  skip   ${id} ${title}`)
    return
  }
  const page = await windowFor(role)
  const began = Date.now()
  const snap = async (label = '') => {
    await page.waitForTimeout(600)
    const name = `${id}${label ? '-' + label : ''}.png`
    await page.screenshot({ path: path.join(OUT, name) })
    record.shots.push(name)
  }
  try {
    try {
      await action(page, snap)
    } catch (error) {
      // The API allows 20 sign-in, sign-up and "who am I" calls a minute from one address. If the page says so, wait it out and do the step once more.
      if (!(await page.getByText('Too many requests').count().catch(() => 0))) throw error
      console.log(`         ${id}: refused by the rate limit, waiting a minute and trying the step again`)
      record.shots.length = 0
      await page.waitForTimeout(65000)
      await action(page, snap)
    }
    if (!record.shots.length) await snap()
    record.ok = true
    console.log(`  ok     ${id} ${title}`)
  } catch (error) {
    record.error = String(error.message ?? error).split('\n')[0]
    try {
      const name = `${id}-FAILED.png`
      await page.screenshot({ path: path.join(OUT, name) })
      record.shots.push(name)
    } catch { /* the window is gone; the text of the error is enough */ }
    console.log(`  FAILED ${id} ${title} - ${record.error}`)
    if (blocks) blockedBy = record
  } finally {
    record.seconds = (Date.now() - began) / 1000
  }
}

async function signIn(page, who) {
  // The API allows 20 sign-in and "who am I" calls a minute from one address, and every page load makes one. After many page loads in a row
  // (for example a second run straight after the first) the sign-in is refused: wait out the minute and try once more.
  for (let attempt = 1; ; attempt++) {
    await page.goto(`${WEB}/login`)
    await page.fill('#email', who.email)
    await page.fill('#password', who.password)
    await page.click('button[type=submit]')
    try {
      await page.waitForURL((u) => !/login/.test(u.pathname), { timeout: 15000 })
      return
    } catch (error) {
      if (attempt >= 2) throw error
      console.log('         sign-in refused, waiting a minute (rate limit) and trying again')
      await page.waitForTimeout(65000)
    }
  }
}
const menu = (page) => page.getByRole('navigation', { name: 'Main' }) // Home cards repeat the menu links, so look in the menu only
const toast = (page, text) => page.getByText(text, { exact: false }).first().waitFor()
const row = (page, selector, text) => page.locator(`${selector}:has-text("${text}")`).first()

// ===================================================================================================================
chapterOf('1. The administrator prepares the platform')

await step({
  role: 'admin', title: 'Sign in as the administrator',
  does: 'Opens the sign-in page, types the administrator e-mail and password and presses Sign in.',
  expect: 'The administrator reaches the Home page and sees the administrator menu (Places, Ad rates, Plans, Users).',
}, async (page, snap) => {
  await page.goto(`${WEB}/login`)
  await snap('sign-in')
  await signIn(page, people.admin)
  await page.getByRole('navigation', { name: 'Main' }).getByRole('link', { name: 'Places' }).waitFor()
})

await step({
  role: 'admin', title: 'Add a city and a group',
  does: `Opens Places, picks India, then Maharashtra, adds the city "${CITY}" and, inside it, the group "${GROUP}".`,
  expect: 'The new city and group appear in their columns. Shops can now be placed here, and ads can be booked for the whole place.',
}, async (page, snap) => {
  await page.goto(`${WEB}/admin/locations`)
  await page.click('[data-testid="country-India"]')
  await page.click('[data-testid="state-Maharashtra"]')
  await page.getByLabel('New City name').fill(CITY)
  await page.getByRole('button', { name: 'Add City' }).click()
  await page.click(`[data-testid="city-${CITY}"]`)
  await page.getByLabel('New Group name').fill(GROUP)
  await page.getByRole('button', { name: 'Add Group' }).click()
  await page.waitForSelector(`[data-testid="group-${GROUP}"]`)
  await snap()
})

await step({
  role: 'admin', title: 'Check the ad price list',
  does: 'Makes sure a price for ads exists (100 an hour, 70% to the shop) and opens Ad rates.',
  expect: 'The list shows a rate for every shop. Ads booked later are priced from it.',
}, async (page, snap) => {
  const token = await apiLogin(people.admin)
  await call(token, 'POST', '/ad-rates', { level: 'All', kind: null, pricePerHour: 100, shopSharePercent: 70 }).catch(() => undefined) // already there is fine
  await page.goto(`${WEB}/admin/ad-rates`)
  await page.waitForSelector('text=Every shop')
  await snap()
})

await step({
  role: 'admin', title: 'Look at the subscription plans',
  does: 'Opens Plans.',
  expect: 'The plans a shop can choose from are listed (for example Silver).',
  blocks: false,
}, async (page, snap) => {
  await page.goto(`${WEB}/admin/subscription-plans`)
  await page.waitForSelector('text=Silver')
  await snap()
})

// ===================================================================================================================
chapterOf('2. A shop owner joins')

let shopId = null
await step({
  role: 'owner', title: 'Create the shop account',
  does: `Opens the sign-up page and registers ${people.owner.name} with the shop "${SHOP}".`,
  expect: 'The account is created and the owner lands on Home, signed in.',
}, async (page, snap) => {
  await page.goto(`${WEB}/register`)
  await page.fill('#firstName', people.owner.first)
  await page.fill('#lastName', people.owner.last)
  await page.fill('#email', people.owner.email)
  await page.fill('#password', people.owner.password)
  await page.fill('#confirmPassword', people.owner.password)
  await page.fill('#shopName', SHOP)
  await snap('form')
  await page.getByRole('button', { name: 'Create account' }).click()
  await page.waitForURL(/dashboard/)
  await menu(page).getByRole('link', { name: 'Banners' }).waitFor()
})

await step({
  role: 'owner', title: 'Choose a subscription plan',
  does: 'Opens Subscription, picks the first plan and presses Subscribe.',
  expect: 'The plan is active ("Subscribed to …") and the page shows the current plan.',
}, async (page, snap) => {
  await page.goto(`${WEB}/subscription`)
  await page.getByRole('radiogroup', { name: 'Plans' }).getByRole('radio').first().click()
  await snap('choose')
  await page.getByRole('button', { name: /^Subscribe/ }).click()
  await toast(page, 'Subscribed to')
  await snap('done')
})

await step({
  role: 'owner', title: 'Place the shop in the new city',
  does: `Puts the shop in "${CITY}" / "${GROUP}" (done through the API here) and opens My shop.`,
  expect: 'The shop page shows the shop. The shop now belongs to the place the administrator created.',
}, async (page, snap) => {
  const admin = await apiLogin(people.admin)
  const owner = await apiLogin(people.owner)
  shopId = JSON.parse(Buffer.from(owner.split('.')[1], 'base64url').toString()).shop_id
  const states = list(await call(admin, 'GET', '/locations/states?countryCode=IN'))
  const state = states.find((s) => s.name === 'Maharashtra')
  const city = list(await call(admin, 'GET', `/locations/cities?stateId=${state.id}`)).find((c) => c.name === CITY)
  const group = list(await call(admin, 'GET', `/locations/groups?cityId=${city.id}`)).find((g) => g.name === GROUP)
  await call(owner, 'PUT', `/locations/shops/${shopId}`, { cityId: Number(city.id), groupId: group ? Number(group.id) : null })
  await page.goto(`${WEB}/shops`)
  await page.waitForSelector(`text=${SHOP}`)
  await snap()
})

await step({
  role: 'owner', title: 'See the default board',
  does: 'Opens Default board.',
  expect: 'A preview shows what the shop screen displays when no banner is scheduled.',
  blocks: false,
}, async (page, snap) => {
  await page.goto(`${WEB}/default-board`)
  await page.waitForSelector('[data-testid="board-preview"]')
  await snap()
})

await step({
  role: 'owner', title: 'Add a sales executive',
  does: `Opens Team and creates a login for ${people.exec.name} with a temporary password.`,
  expect: `${people.exec.name} appears in the list of sales executives.`,
}, async (page, snap) => {
  await page.goto(`${WEB}/team`)
  await page.fill('#exec-first', people.exec.first)
  await page.fill('#exec-last', people.exec.last)
  await page.fill('#exec-email', people.exec.email)
  await page.fill('#exec-password', people.exec.password)
  await snap('form')
  await page.getByRole('button', { name: 'Add login' }).click()
  await page.waitForSelector(`text=${people.exec.first}`)
  await snap('done')
})

// ===================================================================================================================
chapterOf('3. The sales executive makes a banner and sends it for approval')

await step({
  role: 'exec', title: 'Sign in as the sales executive',
  does: 'Signs in with the login the owner created.',
  expect: 'Home opens. The menu is shorter than the owner\'s: no Team, no Subscription.',
}, async (page, snap) => {
  await signIn(page, people.exec)
  await menu(page).getByRole('link', { name: 'Banners' }).waitFor()
  if (await menu(page).getByRole('link', { name: 'Team' }).count()) throw new Error('The executive can see Team, which belongs to the owner')
})

await step({
  role: 'exec', title: 'Create a banner',
  does: `Opens Banners, types the name "${BANNER_1}" and presses Create banner.`,
  expect: 'The banner appears in the list as "Not submitted".',
}, async (page, snap) => {
  await page.goto(`${WEB}/banners`)
  await page.fill('#banner-name', BANNER_1)
  await page.getByRole('button', { name: 'Create banner' }).click()
  await row(page, 'li', BANNER_1).waitFor()
  await snap()
})

await step({
  role: 'exec', title: 'Design the banner in the editor',
  does: 'Opens the editor, adds a text box, types the words and saves.',
  expect: 'The text box shows on the canvas with the typed words and the banner is saved.',
  blocks: false,
}, async (page, snap) => {
  await row(page, 'li', BANNER_1).getByText('Open editor').click()
  await page.locator('button[title="Add text element"]').click()
  await page.waitForSelector('[data-component-id]')
  await page.getByLabel('Content').fill('Fresh bread from 7 am')
  await page.getByRole('button', { name: 'Save', exact: true }).click()
  await page.waitForTimeout(800)
  await snap()
})

await step({
  role: 'exec', title: 'Choose when the banner runs',
  does: 'Back on Banners, presses Set schedule, picks a start (an hour ago) and an end (7 days from now) and saves.',
  expect: 'The banner shows its dates instead of "Not scheduled".',
}, async (page, snap) => {
  await page.goto(`${WEB}/banners`)
  const li = row(page, 'li', BANNER_1)
  await li.getByText('Set schedule').click()
  const inputs = li.locator('input[type=datetime-local]')
  await inputs.nth(0).fill(inDays(0, -1))
  await inputs.nth(1).fill(inDays(7))
  await snap('form')
  await li.getByRole('button', { name: 'Save schedule' }).click()
  await toast(page, 'Schedule saved')
  await snap('done')
})

await step({
  role: 'exec', title: 'Send the banner for approval',
  does: 'Presses Submit for approval.',
  expect: 'The banner shows "Waiting for approval". The executive cannot publish it.',
}, async (page, snap) => {
  const li = row(page, 'li', BANNER_1)
  await li.getByRole('button', { name: 'Submit for approval' }).click()
  await li.getByText('Waiting for approval').waitFor()
  await snap()
})

// ===================================================================================================================
chapterOf('4. The owner approves and publishes')

await step({
  role: 'owner', title: 'See the message about the new banner',
  does: 'Looks at the bell at the top right and opens the messages.',
  expect: 'There is an unread message that the executive sent a banner for approval.',
  blocks: false,
}, async (page, snap) => {
  await page.goto(`${WEB}/dashboard`)
  await page.waitForSelector('[data-testid="bell-count"]')
  await page.getByRole('button', { name: /Messages/ }).click()
  await page.waitForSelector('[data-testid^="message-"]')
  await snap()
})

await step({
  role: 'owner', title: 'Approve the banner',
  does: `Opens Approvals and presses Approve on "${BANNER_1}".`,
  expect: 'A message says "Banner approved." and the banner is now Approved.',
}, async (page, snap) => {
  await page.goto(`${WEB}/approvals`)
  const li = row(page, 'li', BANNER_1)
  await li.waitFor()
  await snap('waiting')
  await li.getByRole('button', { name: 'Approve' }).click()
  await toast(page, 'Banner approved.')
  await snap('approved')
})

await step({
  role: 'owner', title: 'Publish the banner',
  does: 'Presses Publish.',
  expect: 'A message says "Banner published." and the banner shows "Live".',
}, async (page, snap) => {
  const li = row(page, 'li', BANNER_1)
  await li.getByRole('button', { name: 'Publish' }).click()
  await toast(page, 'Banner published.')
  await page.getByRole('tab', { name: 'Live' }).click()
  await row(page, 'li', BANNER_1).getByText('Live').first().waitFor()
  await snap()
})

await step({
  role: 'owner', title: 'See it on the shop screen',
  does: 'Opens Shop screen, the page that plays on the TV in the shop.',
  expect: 'The screen is playing. The published banner is on it.',
  blocks: false,
}, async (page, snap) => {
  await page.goto(`${WEB}/display`)
  await page.waitForSelector('[data-testid="banner-stage"], [data-testid="ad-layout"]')
  await snap()
})

// ===================================================================================================================
chapterOf('5. A banner is sent back, fixed and approved')

await step({
  role: 'exec', title: 'Make a second banner for later',
  does: `Creates "${BANNER_2}", schedules it 8 to 12 days from now and sends it for approval.`,
  expect: 'The second banner shows "Waiting for approval".',
}, async (page, snap) => {
  await page.goto(`${WEB}/banners`)
  await page.fill('#banner-name', BANNER_2)
  await page.getByRole('button', { name: 'Create banner' }).click()
  const li = row(page, 'li', BANNER_2)
  await li.waitFor()
  await li.getByText('Set schedule').click()
  const inputs = li.locator('input[type=datetime-local]')
  await inputs.nth(0).fill(inDays(8))
  await inputs.nth(1).fill(inDays(12))
  await li.getByRole('button', { name: 'Save schedule' }).click()
  await toast(page, 'Schedule saved')
  await li.getByRole('button', { name: 'Submit for approval' }).click()
  await li.getByText('Waiting for approval').waitFor()
  await snap()
})

await step({
  role: 'owner', title: 'Send the second banner back with a reason',
  does: 'Opens Approvals, presses Reject on the second banner and types a reason.',
  expect: 'A message says "Banner rejected." and the banner shows "Rejected".',
}, async (page, snap) => {
  await page.goto(`${WEB}/approvals`)
  const li = row(page, 'li', BANNER_2)
  await li.waitFor()
  await li.getByRole('button', { name: 'Reject' }).click()
  await page.fill('#reason', 'Please add the opening hours to the banner.')
  await snap('reason')
  await page.getByRole('button', { name: 'Reject banner' }).click()
  await toast(page, 'Banner rejected.')
  await snap('done')
})

await step({
  role: 'exec', title: 'Read the reason and send it in again',
  does: 'Opens Approvals, reads the reason and presses Submit for approval.',
  expect: 'The reason is shown. After the new submit the banner is again "Waiting for approval".',
}, async (page, snap) => {
  await page.goto(`${WEB}/approvals`)
  await page.waitForSelector('text=Please add the opening hours')
  await snap('reason')
  const li = row(page, 'li', BANNER_2)
  await li.getByRole('button', { name: 'Submit for approval' }).click()
  await toast(page, 'Submitted for approval.')
  await li.getByText('Waiting for approval').waitFor()
  await snap('resubmitted')
})

await step({
  role: 'owner', title: 'Approve the corrected banner',
  does: 'Presses Approve, then Publish.',
  expect: 'Both banners are now live or scheduled to go live. The history of the second banner shows submit, reject, submit, approve.',
}, async (page, snap) => {
  await page.goto(`${WEB}/approvals`)
  const li = row(page, 'li', BANNER_2)
  await li.getByRole('button', { name: 'Approve' }).click()
  await toast(page, 'Banner approved.')
  await row(page, 'li', BANNER_2).getByRole('button', { name: 'Publish' }).click()
  await toast(page, 'Banner published.')
  await page.getByRole('tab', { name: 'All' }).click()
  await row(page, 'li', BANNER_2).getByRole('button', { name: 'History' }).click()
  await snap()
})

// ===================================================================================================================
chapterOf('6. Ads: the executive books one, the owner approves it')

await step({
  role: 'exec', title: 'Book an ad for a customer',
  does: `Opens Ads, presses Book an ad and first tries a headline with a phone number in it (it is refused), then fills in "${AD_SHOP}", chooses the dates and saves it as a draft.`,
  expect: 'The phone number is refused: ads must not carry personal details. The corrected ad is listed as a Draft with its price per hour.',
}, async (page, snap) => {
  await page.goto(`${WEB}/ads`)
  await page.getByRole('button', { name: 'Book an ad' }).click()
  await page.fill('#ad-advertiser', 'Fresh Milk Dairy')
  await page.fill('#ad-headline', 'Call 9876543210 now')
  await page.fill('#ad-body', 'Milk delivered before breakfast.')
  await page.fill('#ad-start', inDays(0, -1))
  await page.fill('#ad-end', inDays(20))
  await page.getByRole('button', { name: 'Save as draft' }).click()
  const refusal = page.getByRole('alert').filter({ hasText: 'Ads must not carry personal details' })
  await refusal.waitFor()
  await refusal.scrollIntoViewIfNeeded()
  await snap('refused')
  await page.fill('#ad-headline', AD_SHOP)
  await snap('form')
  await page.getByRole('button', { name: 'Save as draft' }).click()
  await page.waitForSelector(`[data-testid="ad-row-${AD_SHOP}"]`)
  await snap('draft')
})

await step({
  role: 'exec', title: 'Send the ad in',
  does: 'Presses Send in.',
  expect: 'The ad shows "Waiting for approval": the owner has to decide.',
}, async (page, snap) => {
  const ad = page.locator(`[data-testid="ad-row-${AD_SHOP}"]`)
  await ad.getByRole('button', { name: 'Send in' }).click()
  await ad.getByText('Waiting for approval').waitFor()
  await snap()
})

await step({
  role: 'owner', title: 'Approve the ad',
  does: 'Opens Ads and presses Approve on the executive\'s ad.',
  expect: 'The ad shows "Approved" and now plays on the shop screen.',
}, async (page, snap) => {
  await page.goto(`${WEB}/ads`)
  const ad = page.locator(`[data-testid="ad-row-${AD_SHOP}"]`)
  await ad.waitFor()
  await snap('waiting')
  await ad.getByRole('button', { name: 'Approve' }).click()
  await ad.getByText('Approved', { exact: true }).waitFor()
  await snap('approved')
})

await step({
  role: 'exec', title: 'See that the ad was approved',
  does: 'Opens Ads again.',
  expect: 'The same ad now shows "Approved". The executive can only cancel it, not change it.',
  blocks: false,
}, async (page, snap) => {
  await page.goto(`${WEB}/ads`)
  await page.locator(`[data-testid="ad-row-${AD_SHOP}"]`).getByText('Approved', { exact: true }).waitFor()
  await snap()
})

// ===================================================================================================================
chapterOf('7. The administrator books an ad and reviews the platform')

await step({
  role: 'admin', title: 'Book an ad on the shop screen',
  does: `Opens Ads, chooses the shop "${SHOP}", books "${AD_ADMIN}" on the left edge and sends it in.`,
  expect: 'The ad is approved straight away (the administrator needs nobody\'s approval) and shows the price and the shop\'s share.',
}, async (page, snap) => {
  await page.goto(`${WEB}/ads`)
  await page.waitForFunction((name) => [...document.querySelectorAll('#ads-shop option')].some((o) => o.textContent.includes(name)), SHOP)
  const value = await page.evaluate((name) => [...document.querySelectorAll('#ads-shop option')].find((o) => o.textContent.includes(name)).value, SHOP)
  await page.selectOption('#ads-shop', value)
  await page.waitForSelector(`[data-testid="ad-row-${AD_SHOP}"]`)
  await snap('shop-ads')
  await page.getByRole('button', { name: 'Book an ad' }).click()
  await page.fill('#ad-advertiser', 'Union Bank')
  await page.fill('#ad-headline', AD_ADMIN)
  await page.fill('#ad-body', 'Zero balance account, open it today.')
  await page.selectOption('#ad-placement', 'Left')
  await page.fill('#ad-start', inDays(0, -1))
  await page.fill('#ad-end', inDays(20))
  await snap('form')
  await page.getByRole('button', { name: 'Save as draft' }).click()
  const ad = page.locator(`[data-testid="ad-row-${AD_ADMIN}"]`)
  await ad.waitFor()
  await ad.getByRole('button', { name: 'Send in' }).click()
  await ad.getByText('Approved', { exact: true }).waitFor()
  await snap('booked')
})

await step({
  role: 'admin', title: 'Find the new users and shop',
  does: 'Checks that the owner, the executive and the shop exist, then opens Subscriptions.',
  expect: 'Both users exist, and the shop is listed with its plan.',
  blocks: false,
}, async (page, snap) => {
  const token = await apiLogin(people.admin)
  for (const who of [people.owner, people.exec]) {
    const found = list(await call(token, 'GET', `/admin/users?search=${encodeURIComponent(who.email)}&pageSize=5`)).some((u) => u.email === who.email)
    if (!found) throw new Error(`${who.email} is not among the users`)
  }
  await page.goto(`${WEB}/admin/subscriptions`)
  await page.waitForSelector(`text=${SHOP}`)
  await snap()
})

await step({
  role: 'admin', title: 'Open the ad report',
  does: 'Opens Ad report.',
  expect: 'The report for the platform opens.',
  blocks: false,
}, async (page, snap) => {
  await page.goto(`${WEB}/admin/ad-reports`)
  await page.waitForSelector('text=Ad report')
  await snap()
})

await step({
  role: 'admin', title: 'Read the activity log',
  does: 'Opens Activity.',
  expect: 'The log shows what happened in this story: the sign-ups, the approvals and the ads.',
  blocks: false,
}, async (page, snap) => {
  await page.goto(`${WEB}/admin/audit-log`)
  await page.waitForSelector('text=Activity log')
  await snap()
})

// ===================================================================================================================
chapterOf('8. The result')

await step({
  role: 'owner', title: 'The shop screen with the banner and both ads',
  does: 'Opens Shop screen.',
  expect: 'The banner plays in the middle. The executive\'s ad is on the right, the administrator\'s on the left.',
}, async (page, snap) => {
  await page.goto(`${WEB}/display`)
  await page.waitForSelector('[data-testid="ad-layout"]')
  await page.waitForSelector(`text=${AD_ADMIN}`)
  await page.waitForSelector(`text=${AD_SHOP}`)
  await snap()
})

await step({
  role: 'owner', title: 'The monthly statement',
  does: 'Opens the monthly statement from Ads.',
  expect: 'The statement lists the ads that ran and what the shop earns.',
  blocks: false,
}, async (page, snap) => {
  await page.goto(`${WEB}/ads/statement`)
  await page.waitForSelector('[data-testid^="statement-"], [data-testid="statement-empty"]')
  await snap()
})

// ===================================================================================================================
chapterOf('9. Around the shop: calendar, files, the shop television and a safer sign-in')

// a picture to upload, drawn by the browser so the demo needs no file of its own
const PICTURE = `fresh-bread-${RUN}.png`
let pictureBytes = null
{
  const drawing = await browser.newPage({ viewport: { width: 640, height: 360 } })
  await drawing.setContent('<body style="margin:0;background:#f59e0b;display:flex;align-items:center;justify-content:center;height:360px;font:700 56px sans-serif;color:#fff">Fresh bread</body>')
  pictureBytes = await drawing.screenshot({ type: 'png' })
  await drawing.close()
}

await step({
  role: 'owner', title: 'Look at the schedule calendar',
  does: 'Opens Calendar.',
  expect: `The week shows "${BANNER_1}" as a coloured block on the days it runs. Between banners the shop shows its default board.`,
  blocks: false,
}, async (page, snap) => {
  await page.goto(`${WEB}/banners/calendar`)
  await page.waitForSelector('[data-testid="calendar-range"]')
  await page.waitForSelector(`[data-testid="block-${BANNER_1}"]`)
  await snap()
})

await step({
  role: 'owner', title: 'Upload a picture to My files',
  does: `Opens My files, presses Upload a file and chooses the picture "${PICTURE}".`,
  expect: 'A message says the picture was uploaded. It appears in the library with "Not used", and the storage bar shows what is used of the plan.',
  blocks: false,
}, async (page, snap) => {
  await page.goto(`${WEB}/media`)
  await page.waitForSelector('[data-testid="storage-usage"]')
  await snap('empty')
  await page.setInputFiles('input[aria-label="Upload a file"]', { name: PICTURE, mimeType: 'image/png', buffer: pictureBytes })
  await toast(page, `${PICTURE} uploaded`)
  await page.waitForSelector(`[data-testid="file-${PICTURE}"]`)
  await snap('uploaded')
})

await step({
  role: 'exec', title: 'Use the shop\'s files',
  does: 'Opens My files.',
  expect: `The picture the owner uploaded is there too: the library belongs to the shop, so every login of the shop can use it in a banner.`,
  blocks: false,
}, async (page, snap) => {
  await page.goto(`${WEB}/media`)
  await page.waitForSelector(`[data-testid="file-${PICTURE}"]`)
  await snap()
})

await step({
  role: 'exec', title: 'Look at the schedule calendar',
  does: 'Opens Calendar.',
  expect: `The executive sees the same week as the owner, with "${BANNER_1}" on it.`,
  blocks: false,
}, async (page, snap) => {
  await page.goto(`${WEB}/banners/calendar`)
  await page.waitForSelector(`[data-testid="block-${BANNER_1}"]`)
  await snap()
})

let pairingCode = null
await step({
  role: 'screen', title: 'The television shows a code',
  does: 'On the television, opens the web browser and goes to the Player page (the address is shown on the Screens page). Nobody signs in.',
  expect: 'The television shows a code of six characters and says how to add it to the shop.',
}, async (page, snap) => {
  await page.goto(`${WEB}/player?reset=1`)
  await page.waitForSelector('[data-testid="pairing-code"]')
  pairingCode = (await page.locator('[data-testid="pairing-code"]').innerText()).replace(/\s/g, '')
  if (pairingCode.length !== 6) throw new Error(`Expected a code of six characters, the screen shows "${pairingCode}"`)
  await snap()
})

await step({
  role: 'owner', title: 'Add the screen to the shop',
  does: 'Opens Screens, types the code from the television, names the screen "Shop window" and presses Add the screen.',
  expect: 'A message says the screen was added, and the screen is listed under its name.',
}, async (page, snap) => {
  await page.goto(`${WEB}/screens`)
  await page.waitForSelector('[data-testid="no-screen"]')
  await page.fill('#screen-code', pairingCode)
  await page.fill('#screen-name', 'Shop window')
  await snap('form')
  await page.getByRole('button', { name: 'Add the screen' }).click()
  await toast(page, 'The screen was added')
  await page.waitForSelector('[data-testid="screen-Shop window"]')
  await snap('added')
})

await step({
  role: 'screen', title: 'The television starts playing by itself',
  does: 'Waits a few seconds on the television.',
  expect: 'The code disappears and the television plays the shop\'s published banner and ads.',
}, async (page, snap) => {
  await page.waitForSelector('[data-testid="banner-stage"], [data-testid="ad-layout"]', { timeout: 60000 })
  await snap()
})

await step({
  role: 'owner', title: 'See that the screen is online',
  does: 'Opens Screens again.',
  expect: 'The screen shows "Online" and when it was last heard from. "What was shown" fills in as the screen reports, once a minute, with the hours each banner and ad was on it.',
  blocks: false,
}, async (page, snap) => {
  await page.goto(`${WEB}/screens`)
  await page.locator('[data-testid="screen-Shop window"] [data-testid="screen-state"]').getByText('Online', { exact: true }).waitFor({ timeout: 70000 })
  await snap()
})

// ----- two-step sign-in. The code from the authenticator app is worked out here the same way the app does it (RFC 6238).
const base32 = 'ABCDEFGHIJKLMNOPQRSTUVWXYZ234567'
function totp(secret, stepOffset = 0) {
  let bits = ''
  for (const c of secret.replace(/[\s=]/g, '').toUpperCase()) bits += base32.indexOf(c).toString(2).padStart(5, '0')
  const key = Buffer.from(bits.match(/.{8}/g).map((b) => parseInt(b, 2)))
  const counter = Buffer.alloc(8)
  counter.writeBigUInt64BE(BigInt(Math.floor(Date.now() / 30000) + stepOffset))
  const h = crypto.createHmac('sha1', key).update(counter).digest()
  const o = h[h.length - 1] & 0xf
  return String(((h.readUInt32BE(o) & 0x7fffffff) % 1000000)).padStart(6, '0')
}

let ownerSecret = null
await step({
  role: 'owner', title: 'Turn on two-step sign-in',
  does: 'Opens the account page (the name at the top right), presses Set up two-step sign-in, types the password again, scans the picture with the authenticator app and types the first code.',
  expect: 'Ten recovery codes are shown once, and the page says two-step sign-in is on.',
}, async (page, snap) => {
  await page.goto(`${WEB}/account`)
  await page.waitForSelector('[data-testid="two-factor-status"]')
  await snap('account')
  await page.getByRole('button', { name: 'Set up two-step sign-in' }).click()
  await page.fill('#tf-password', people.owner.password)
  await page.getByRole('button', { name: 'Continue' }).click()
  await page.waitForSelector('[data-testid="two-factor-secret"]')
  ownerSecret = (await page.locator('[data-testid="two-factor-secret"]').innerText()).trim()
  await snap('scan')
  await page.fill('#tf-code', totp(ownerSecret))
  await page.getByRole('button', { name: 'Turn it on' }).click()
  await page.waitForSelector('[data-testid="recovery-codes"]')
  if ((await page.locator('[data-testid="recovery-codes"] li').count()) !== 10) throw new Error('Expected ten recovery codes')
  await snap('codes')
  await page.getByRole('button', { name: 'I have saved them' }).click()
  await page.getByText('It is on.', { exact: false }).waitFor()
  await snap('on')
})

await step({
  role: 'owner', title: 'Sign in again with a code',
  does: 'Signs out, signs in with the e-mail and password, and then types the six-digit code the app shows.',
  expect: 'After the password the page asks for a code. With the right code the owner is back on Home.',
}, async (page, snap) => {
  await page.getByText('Sign out', { exact: true }).click()
  await page.waitForURL(/login/)
  // Two sign-in calls and the page loads in between can run into the limit of 20 a minute (see signIn above): wait it out and start again once.
  const refused = page.getByText('Too many requests')
  for (let attempt = 1; ; attempt++) {
    if (attempt > 1) await page.goto(`${WEB}/login`)
    await page.fill('#email', people.owner.email)
    await page.fill('#password', people.owner.password)
    await page.click('button[type=submit]')
    await page.waitForSelector('#code, :text("Too many requests")')
    if (!(await refused.count())) {
      if (attempt === 1) await snap('code')
      await page.fill('#code', totp(ownerSecret, 1)) // the next 30 seconds: a code that was used to turn it on is refused a second time
      await page.click('button[type=submit]')
      try {
        await page.waitForURL((u) => !/login/.test(u.pathname), { timeout: 8000 })
        break
      } catch (error) {
        if (!(await refused.count())) throw error
      }
    }
    if (attempt >= 2) throw new Error('Sign-in was refused twice with "Too many requests"')
    console.log('         sign-in refused, waiting a minute (rate limit) and trying again')
    await page.waitForTimeout(65000)
  }
  await menu(page).getByRole('link', { name: 'Banners' }).waitFor()
  await snap('home')
})

await browser.close()

// ===================================================================================================================
// The Word file
const failed = results.filter((r) => !r.ok && !r.skipped)
const skipped = results.filter((r) => r.skipped)
const passed = results.filter((r) => r.ok)
const verdict = failed.length + skipped.length === 0 ? 'ALL STEPS WORKED' : `${failed.length} FAILED, ${skipped.length} SKIPPED`

fs.writeFileSync(path.join(OUT, 'results.json'), JSON.stringify({ run: RUN, finishedAt: new Date().toISOString(), verdict, shop: SHOP, results }, null, 2))

const cell = (text, o = {}) => new TableCell({
  width: o.width ? { size: o.width, type: WidthType.PERCENTAGE } : undefined,
  shading: o.fill ? { type: ShadingType.CLEAR, fill: o.fill } : undefined,
  margins: { top: 60, bottom: 60, left: 100, right: 100 },
  children: [new Paragraph({ children: [new TextRun({ text: String(text), bold: o.bold, color: o.color, size: 20 })] })],
})
const table = (rows, widths) => new Table({
  width: { size: 100, type: WidthType.PERCENTAGE },
  rows: rows.map((r, i) => new TableRow({
    tableHeader: i === 0,
    children: r.map((c, j) => (typeof c === 'object' ? cell(c.text, { ...c, width: widths?.[j] }) : cell(c, { bold: i === 0, fill: i === 0 ? 'E5E7EB' : undefined, width: widths?.[j] }))),
  })),
})
const outcome = (r) => (r.ok ? { text: 'Worked', color: '15803D', bold: true } : r.skipped ? { text: 'Skipped', color: '92400E', bold: true } : { text: 'FAILED', color: 'B91C1C', bold: true })
const para = (text, o = {}) => new Paragraph({ spacing: { after: 100 }, children: [new TextRun({ text, ...o })] })

function picture(file) {
  const data = fs.readFileSync(path.join(OUT, file))
  const w = data.readUInt32BE(16)
  const h = data.readUInt32BE(20)
  return new Paragraph({
    spacing: { before: 80, after: 160 }, alignment: AlignmentType.CENTER,
    children: [new ImageRun({ type: 'png', data, transformation: { width: 600, height: Math.round((600 * h) / w) }, altText: { title: file, description: file, name: file } })],
  })
}

const body = [
  new Paragraph({ heading: HeadingLevel.TITLE, children: [new TextRun('BannerAI: the whole story, start to finish')] }),
  para(`Run ${RUN}, ${new Date().toLocaleString()}. Every role takes part: the administrator, a shop owner and a sales executive.`, { color: '4B5563' }),
  new Paragraph({ heading: HeadingLevel.HEADING_1, children: [new TextRun('Summary')] }),
  table([
    ['Verdict', { text: verdict, bold: true, color: failed.length + skipped.length ? 'B91C1C' : '15803D' }],
    ['Steps', String(results.length)],
    ['Worked', String(passed.length)],
    ['Failed', String(failed.length)],
    ['Skipped (an earlier step failed)', String(skipped.length)],
    ['Shop made for this run', SHOP],
    ['Owner / executive', `${people.owner.email} / ${people.exec.email}`],
  ], [35, 65]),
  para(''),
]
if (failed.length || skipped.length) {
  body.push(new Paragraph({ children: [new TextRun({ text: 'What needs attention', bold: true, color: 'B91C1C' })] }))
  for (const r of [...failed, ...skipped]) body.push(para(`•  Step ${r.n}, ${people[r.role].label}: ${r.title}. ${r.error}`))
  body.push(para(''))
}
body.push(new Paragraph({ heading: HeadingLevel.HEADING_2, children: [new TextRun('The story at a glance')] }))
body.push(table([
  ['#', 'Who', 'Step', 'Result'],
  ...results.map((r) => [String(r.n), people[r.role].label, r.title, outcome(r)]),
], [6, 22, 52, 20]))
body.push(new Paragraph({ children: [new PageBreak()] }))

let lastChapter = ''
for (const r of results) {
  if (r.chapter !== lastChapter) {
    body.push(new Paragraph({ heading: HeadingLevel.HEADING_1, spacing: { before: 240 }, children: [new TextRun(r.chapter)] }))
    lastChapter = r.chapter
  }
  body.push(new Paragraph({ heading: HeadingLevel.HEADING_2, spacing: { before: 240 }, children: [new TextRun(`Step ${r.n}. ${r.title}`)] }))
  body.push(new Paragraph({ spacing: { after: 100 }, children: [new TextRun({ text: `${people[r.role].label} (${people[r.role].name})   `, bold: true }), new TextRun({ ...outcome(r), text: outcome(r).text })] }))
  body.push(new Paragraph({ spacing: { after: 80 }, children: [new TextRun({ text: 'What is done: ', bold: true }), new TextRun(r.does)] }))
  body.push(new Paragraph({ spacing: { after: 80 }, children: [new TextRun({ text: 'What must be true: ', bold: true }), new TextRun(r.expect)] }))
  if (r.error) body.push(para(r.error, { color: 'B91C1C', bold: true }))
  for (const file of r.shots) body.push(picture(file))
}

const doc = new Document({
  creator: 'BannerAI', title: 'BannerAI end-to-end demo',
  styles: { default: { document: { run: { font: 'Calibri', size: 22 } } } },
  sections: [{ children: body }],
})
fs.writeFileSync(DOCX, await Packer.toBuffer(doc))

console.log(`\n${verdict}. ${passed.length} of ${results.length} steps worked.`)
console.log(`Word file: ${DOCX}`)
console.log(`Pictures:  ${OUT}`)
process.exit(failed.length + skipped.length ? 1 : 0)

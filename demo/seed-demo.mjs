// Fills a running BannerAI with a small, believable demo: one shop with a team, banners in every state, ads, and messages.
//
//   node demo/seed-demo.mjs                       (API on http://localhost:5000)
//   API=http://localhost:5000 ADMIN_EMAIL=... ADMIN_PASSWORD=... node demo/seed-demo.mjs
//
// It needs an administrator login first:  dotnet BannerService.dll --create-admin <email> <password>
// (with docker compose:  docker compose run --rm api --create-admin demo-admin@bannerai.demo "Demo-Admin-2026!")
//
// Everything is made through the same API the screens use, so the demo shows the real behaviour. Run it once on a fresh database; it stops
// when the demo shop already exists.

const API = (process.env.API ?? 'http://localhost:5000') + '/api'
const ADMIN_EMAIL = process.env.ADMIN_EMAIL ?? 'demo-admin@bannerai.demo'
const ADMIN_PASSWORD = process.env.ADMIN_PASSWORD ?? 'Demo-Admin-2026!'

export const DEMO = {
  admin: { email: ADMIN_EMAIL, password: ADMIN_PASSWORD },
  owner: { email: 'demo-owner@bannerai.demo', password: 'Demo-Owner-2026!', name: 'Olivia Owner' },
  executive: { email: 'demo-seller@bannerai.demo', password: 'Demo-Seller-2026!', name: 'Sam Seller' },
  shop: 'Sunrise Cafe',
}

async function call(token, method, path, body) {
  const response = await fetch(API + path, {
    method,
    headers: { 'Content-Type': 'application/json', 'X-Requested-With': 'XMLHttpRequest', ...(token ? { Authorization: `Bearer ${token}` } : {}) },
    body: body === undefined ? undefined : JSON.stringify(body),
  })
  const text = await response.text()
  let data = null
  try { data = text ? JSON.parse(text) : null } catch { data = text }
  if (!response.ok) throw new Error(`${method} ${path} -> ${response.status} ${typeof data === 'string' ? data : JSON.stringify(data)}`)
  return data && typeof data === 'object' && 'data' in data && Object.keys(data).every((k) => ['success', 'data', 'message'].includes(k)) ? data.data : data
}

const login = async (who) => (await call(null, 'POST', '/authentication/login', { email: who.email, password: who.password })).tokens.accessToken
const day = (offset, hour = 0) => { const d = new Date(); d.setUTCHours(hour, 0, 0, 0); d.setUTCDate(d.getUTCDate() + offset); return d.toISOString() }
const step = (text) => console.log('•', text)

async function banner(token, name, components) {
  const created = await call(token, 'POST', '/banners', { name, description: '', width: 1280, height: 720 })
  let z = 1
  for (const c of components) {
    await call(token, 'POST', `/banners/${created.id}/components`, { zIndex: z++, ...c })
  }
  return created.id
}

const text = (content, x, y, w, h, extra = {}) => ({
  componentType: 1, positionX: x, positionY: y, sizeWidth: w, sizeHeight: h,
  properties: { content, fontSize: 64, fontFamily: 'Arial', fontWeight: '700', color: '#ffffff', textAlign: 'center', lineHeight: 1.3, rotation: 0, opacity: 1, isVisible: true, ...extra },
})
const shape = (x, y, w, h, fillColor) => ({
  componentType: 4, positionX: x, positionY: y, sizeWidth: w, sizeHeight: h,
  properties: { shapeType: 'rectangle', fillColor, strokeColor: fillColor, strokeWidth: 0, rotation: 0, opacity: 1, isVisible: true },
})

async function main() {
  step('Signing in as the administrator')
  const admin = await login(DEMO.admin).catch(() => {
    throw new Error('The administrator could not sign in. Create it first: dotnet BannerService.dll --create-admin <email> <password>')
  })

  const existing = await call(admin, 'GET', '/admin/users?search=demo-owner&pageSize=5')
  if ((existing.items ?? []).some((u) => u.email === DEMO.owner.email)) {
    console.log('The demo shop already exists. Nothing to do.')
    return
  }

  step('Admin: a city, a group and the ad price list')
  const states = await call(admin, 'GET', '/locations/states?countryCode=IN')
  const state = (Array.isArray(states) ? states : states.items).find((s) => s.name === 'Maharashtra')
  const city = await call(admin, 'POST', '/locations/cities', { stateId: Number(state.id), name: 'Mumbai' }).catch(async () => {
    const all = await call(admin, 'GET', `/locations/cities?stateId=${state.id}`)
    return (Array.isArray(all) ? all : all.items).find((c) => c.name === 'Mumbai')
  })
  const group = await call(admin, 'POST', '/locations/groups', { cityId: Number(city.id), name: 'Andheri' }).catch(() => null)
  await call(admin, 'POST', '/ad-rates', { level: 'All', kind: null, pricePerHour: 100, shopSharePercent: 70 }).catch(() => undefined)

  step('Owner signs up with the shop "Sunrise Cafe"')
  const reg = await call(null, 'POST', '/authentication/register', {
    email: DEMO.owner.email, password: DEMO.owner.password, firstName: 'Olivia', lastName: 'Owner', shopName: DEMO.shop,
  })
  let owner = reg.tokens.accessToken
  const shopId = JSON.parse(Buffer.from(owner.split('.')[1], 'base64url').toString()).shop_id

  step('Owner fills in the shop, places it in Mumbai, designs the default board, takes a plan')
  await call(owner, 'PUT', `/shops/${shopId}`, { name: DEMO.shop, description: 'Coffee and fresh bread', address: '12 Seaside Road, Andheri West', postalCode: '400053', phoneNumber: '+912255550100', status: 1 })
  await call(owner, 'PUT', `/locations/shops/${shopId}`, { cityId: Number(city.id), groupId: group ? Number(group.id) : null })
  await call(owner, 'PUT', `/shops/${shopId}/default-board`, { message: 'Open every day, 7 am to 10 pm', background: '#7c2d12', textColor: '#ffedd5', logoMediaFileId: null })
  const plans = await call(owner, 'GET', '/subscriptions/plans')
  const silver = plans.find((p) => p.name === 'Silver') ?? plans[0]
  await call(owner, 'POST', '/subscriptions', { shopId, planId: silver.id, billingPeriod: 1 })

  step('Owner adds a sales executive')
  await call(owner, 'POST', `/shops/${shopId}/team/executives`, { email: DEMO.executive.email, password: DEMO.executive.password, firstName: 'Sam', lastName: 'Seller' })
  const seller = await login(DEMO.executive)

  step('Owner makes the live banner "Welcome Offer" and publishes it')
  owner = await login(DEMO.owner) // a fresh token that carries the owner role
  const welcome = await banner(owner, 'Welcome Offer', [
    shape(0, 0, 1280, 720, '#9a3412'),
    text('Welcome to Sunrise Cafe', 80, 180, 1120, 120, { fontSize: 88 }),
    text('Fresh coffee. Warm bread. Good mornings.', 140, 340, 1000, 80, { fontSize: 48, fontWeight: '400', color: '#fed7aa' }),
  ])
  await call(owner, 'PUT', `/banners/${welcome}/schedule`, { startAt: day(-1), endAt: day(7) })
  const welcomeFlow = (await call(owner, 'POST', '/publish-workflow/initiate', { bannerId: welcome })).id
  await call(owner, 'POST', `/publish-workflow/${welcomeFlow}/submit`)
  await call(owner, 'POST', `/publish-workflow/${welcomeFlow}/approve`, { comment: 'Looks great' })
  await call(owner, 'POST', `/publish-workflow/${welcomeFlow}/publish`)

  step('The executive makes "Weekend Special" and sends it for approval (it waits for the owner)')
  const weekend = await banner(seller, 'Weekend Special', [
    shape(0, 0, 1280, 720, '#14532d'),
    text('Weekend Special', 80, 160, 1120, 120, { fontSize: 96 }),
    text('Any cake and a coffee for 199', 140, 330, 1000, 80, { fontSize: 52, fontWeight: '400', color: '#bbf7d0' }),
  ])
  await call(seller, 'PUT', `/banners/${weekend}/schedule`, { startAt: day(7), endAt: day(14) })
  const weekendFlow = (await call(seller, 'POST', '/publish-workflow/initiate', { bannerId: weekend })).id
  await call(seller, 'POST', `/publish-workflow/${weekendFlow}/submit`)

  step('Ads: an owner ad (live), an administrator ad (live), an executive ad (waits for the owner)')
  const adBody = (o) => ({ background: '#fff7ed', textColor: '#431407', popupSeconds: 0, popupEveryMinutes: 0, spacePercent: 0, startAt: day(-1), endAt: day(20), ...o })
  const ownerAd = await call(owner, 'POST', '/shop-ads', adBody({ advertiserName: 'Bake House', headline: 'Fresh bread at 7 am', body: 'Baked next door, every morning.', kind: 'Side', placement: 'Left', spacePercent: 20 }))
  await call(owner, 'POST', `/shop-ads/${ownerAd.id}/submit`)
  const adminAd = await call(admin, 'POST', '/shop-ads', adBody({ shopId, advertiserName: 'City Savings Bank', headline: 'Open an account today', body: 'Zero balance. Quick and easy.', kind: 'Side', placement: 'Right', spacePercent: 20, background: '#eff6ff', textColor: '#1e3a8a' }))
  await call(admin, 'POST', `/shop-ads/${adminAd.id}/submit`)
  const sellerAd = await call(seller, 'POST', '/shop-ads', adBody({ advertiserName: 'Juice Corner', headline: 'Cold juice, 49', kind: 'Minor', placement: 'BottomRight', background: '#ecfccb', textColor: '#365314' }))
  await call(seller, 'POST', `/shop-ads/${sellerAd.id}/submit`)

  console.log('\nDemo ready. Sign in at the web app with:')
  for (const [role, who] of Object.entries({ Administrator: DEMO.admin, 'Shop owner': DEMO.owner, 'Sales executive': DEMO.executive })) {
    console.log(`  ${role.padEnd(16)} ${who.email}   ${who.password}`)
  }
}

main().catch((error) => {
  console.error('\nThe demo could not be set up:', error.message)
  process.exit(1)
})

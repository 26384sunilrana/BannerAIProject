import { ApiComponent, nextFreeZIndex, toApiBanner, toApiComponent, toBannerComponent } from '@/api/bannerMapper'

const apiComponent = (overrides: Partial<ApiComponent>): ApiComponent => ({
  id: 'c',
  bannerId: 'b',
  componentType: 1,
  positionX: 0,
  positionY: 0,
  sizeWidth: 10,
  sizeHeight: 10,
  zIndex: 0,
  properties: {},
  createdAt: 'now',
  ...overrides,
})

describe('component mapping', () => {
  it('round-trips a component through the API shape', () => {
    const original = {
      type: 'graphics' as const,
      x: 12,
      y: 34,
      width: 56,
      height: 78,
      zIndex: 4,
      rotation: 30,
      opacity: 0.8,
      isVisible: false,
      data: { shapeType: 'circle', fillColor: '#ff0000', strokeColor: '#000000', strokeWidth: 2 },
    }

    const api = toApiComponent(original)
    const back = toBannerComponent(
      apiComponent({
        componentType: api.componentType,
        positionX: api.positionX,
        positionY: api.positionY,
        sizeWidth: api.sizeWidth,
        sizeHeight: api.sizeHeight,
        zIndex: api.zIndex,
        properties: api.properties,
      })
    )

    expect(back).toMatchObject(original)
  })

  it('defaults appearance when the API has none stored', () => {
    const component = toBannerComponent(
      apiComponent({ componentType: 2, properties: { mediaUrl: 'https://x.example.com/a.png' } })
    )

    expect(component).toMatchObject({
      type: 'image',
      rotation: 0,
      opacity: 1,
      isVisible: true,
      data: { mediaUrl: 'https://x.example.com/a.png' },
    })
  })

  it('keeps playlist and other custom properties inside data', () => {
    const component = toBannerComponent(
      apiComponent({
        componentType: 3,
        properties: { playlist: [{ mediaUrl: 'https://x.example.com/v.mp4' }], rotationMode: 'playFull' },
      })
    )

    expect(component.type).toBe('video')
    expect((component.data as any).rotationMode).toBe('playFull')
  })

  it('treats an unknown type as graphics and fractions as whole numbers', () => {
    expect(toBannerComponent(apiComponent({ componentType: 99 })).type).toBe('graphics')
    expect(toApiComponent({ type: 'text', x: 10.6, y: 0.2, width: 99.5, height: 10, zIndex: 1.4, data: {} })).toMatchObject({
      positionX: 11,
      positionY: 0,
      sizeWidth: 100,
      zIndex: 1,
    })
  })
})

describe('banner mapping', () => {
  it('trims the name and keeps size within limits', () => {
    expect(toApiBanner({ title: '  Sale  ', description: '', width: 99999, height: 0 })).toEqual({
      name: 'Sale',
      description: '',
      width: 5000,
      height: 1,
    })
  })
})

describe('nextFreeZIndex', () => {
  it('starts at 0 and goes above the highest layer', () => {
    expect(nextFreeZIndex([])).toBe(0)
    expect(nextFreeZIndex([0, 1, 5])).toBe(6)
  })

  it('reuses a gap when the top layer is taken, and returns null when full', () => {
    expect(nextFreeZIndex([0, 2, 100])).toBe(1)
    expect(nextFreeZIndex(Array.from({ length: 101 }, (_, i) => i))).toBeNull()
  })
})

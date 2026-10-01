import { bannerService } from '@/api/bannerService'
import * as client from '@/api/client'

const summary = {
  id: 'b1',
  shopId: 's1',
  name: 'Summer Sale',
  description: 'desc',
  width: 1200,
  height: 600,
  isPublished: false,
  createdAt: '2026-10-01T00:00:00Z',
  updatedAt: '2026-10-01T00:00:00Z',
}

const apiComponent = (overrides: Record<string, unknown> = {}) => ({
  id: 'c1',
  bannerId: 'b1',
  componentType: 1,
  positionX: 10,
  positionY: 20,
  sizeWidth: 200,
  sizeHeight: 100,
  zIndex: 3,
  properties: { content: 'Hello', rotation: 15, opacity: 0.5, isVisible: false },
  createdAt: '2026-10-01T00:00:00Z',
  ...overrides,
})

describe('bannerService', () => {
  afterEach(() => jest.restoreAllMocks())

  it('loads the banner and its components in the editor shape', async () => {
    const get = jest.spyOn(client.apiClient, 'get').mockImplementation(async (path: string) =>
      (path.endsWith('/preview') ? { components: [apiComponent({ id: 'c2', zIndex: 5 }), apiComponent()] } : summary) as never
    )

    const banner = await bannerService.getBanner('b1')

    expect(get).toHaveBeenCalledWith('/banners/b1')
    expect(get).toHaveBeenCalledWith('/banners/b1/preview')
    expect(banner.title).toBe('Summer Sale')
    expect(banner.components.map((c) => c.id)).toEqual(['c1', 'c2']) // by layer
    expect(banner.components[0]).toMatchObject({
      type: 'text',
      x: 10,
      y: 20,
      width: 200,
      height: 100,
      rotation: 15,
      opacity: 0.5,
      isVisible: false,
      data: { content: 'Hello' },
    })
  })

  it('updates only the given fields and sends the rest unchanged', async () => {
    jest.spyOn(client.apiClient, 'get').mockResolvedValue(summary)
    const put = jest.spyOn(client.apiClient, 'put').mockResolvedValue(undefined)

    await bannerService.updateBanner('b1', { title: 'Winter Sale' })

    expect(put).toHaveBeenCalledWith('/banners/b1', { name: 'Winter Sale', description: 'desc', width: 1200, height: 600 })
  })

  it('adds a component using the API field names', async () => {
    const post = jest.spyOn(client.apiClient, 'post').mockResolvedValue(apiComponent({ id: 'new' }))

    const created = await bannerService.addComponent('b1', {
      type: 'text',
      x: 5,
      y: 6,
      width: 100,
      height: 40,
      zIndex: 2,
      data: { content: 'Hi' },
    })

    expect(post).toHaveBeenCalledWith('/banners/b1/components', {
      componentType: 1,
      positionX: 5,
      positionY: 6,
      sizeWidth: 100,
      sizeHeight: 40,
      zIndex: 2,
      properties: { content: 'Hi', rotation: 0, opacity: 1, isVisible: true },
    })
    expect(created.id).toBe('new')
  })

  it('updates and deletes a component', async () => {
    const put = jest.spyOn(client.apiClient, 'put').mockResolvedValue(apiComponent())
    const del = jest.spyOn(client.apiClient, 'delete').mockResolvedValue(undefined)

    await bannerService.updateComponent('b1', 'c1', {
      type: 'graphics',
      x: -10,
      y: 0,
      width: 9000,
      height: 0,
      zIndex: 500,
      data: { fillColor: '#ffffff' },
    })
    await bannerService.deleteComponent('b1', 'c1')

    // values outside what the API accepts are brought inside it
    expect(put).toHaveBeenCalledWith(
      '/banners/b1/components/c1',
      expect.objectContaining({ componentType: 4, positionX: 0, sizeWidth: 5000, sizeHeight: 1, zIndex: 100 })
    )
    expect(del).toHaveBeenCalledWith('/banners/b1/components/c1')
  })
})

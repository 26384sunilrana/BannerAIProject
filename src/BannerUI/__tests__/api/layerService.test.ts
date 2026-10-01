import { layerService } from '@/api/layerService'
import * as client from '@/api/client'

describe('layerService', () => {
  const order = { componentId: 'c1', oldZIndex: 1, newZIndex: 5 }
  let post: jest.SpyInstance

  beforeEach(() => {
    post = jest.spyOn(client.apiClient, 'post').mockResolvedValue(order)
  })

  afterEach(() => jest.restoreAllMocks())

  it('reorders to a layer', async () => {
    const result = await layerService.reorderComponent('b1', 'c1', 5)

    expect(result).toEqual(order)
    expect(post).toHaveBeenCalledWith('/banners/b1/components/c1/reorder', { newZIndex: 5 })
  })

  it.each([
    ['moveForward', 'move-forward'],
    ['moveBackward', 'move-backward'],
    ['sendToFront', 'send-to-front'],
    ['sendToBack', 'send-to-back'],
  ] as const)('%s calls the %s endpoint', async (method, path) => {
    await layerService[method]('b1', 'c1')

    expect(post).toHaveBeenCalledWith(`/banners/b1/components/c1/${path}`, {})
  })
})

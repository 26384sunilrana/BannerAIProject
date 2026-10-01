import { apiClient } from './client'

export interface LayerOrder {
  componentId: string
  oldZIndex: number
  newZIndex: number
}

const layerPath = (bannerId: string, componentId: string, action: string) =>
  `/banners/${bannerId}/components/${componentId}/${action}`

/** Layer changes are made by the server so two components never end up on the same layer. */
export const layerService = {
  /** Puts the component on the given layer; a component already there swaps places with it. */
  reorderComponent(bannerId: string, componentId: string, newZIndex: number): Promise<LayerOrder> {
    return apiClient.post<LayerOrder>(layerPath(bannerId, componentId, 'reorder'), { newZIndex })
  },

  moveForward(bannerId: string, componentId: string): Promise<LayerOrder> {
    return apiClient.post<LayerOrder>(layerPath(bannerId, componentId, 'move-forward'), {})
  },

  moveBackward(bannerId: string, componentId: string): Promise<LayerOrder> {
    return apiClient.post<LayerOrder>(layerPath(bannerId, componentId, 'move-backward'), {})
  },

  sendToFront(bannerId: string, componentId: string): Promise<LayerOrder> {
    return apiClient.post<LayerOrder>(layerPath(bannerId, componentId, 'send-to-front'), {})
  },

  sendToBack(bannerId: string, componentId: string): Promise<LayerOrder> {
    return apiClient.post<LayerOrder>(layerPath(bannerId, componentId, 'send-to-back'), {})
  },
}

import { apiClient } from './client'
import { GroupShop, LocationItem, LocationLevel, NewLocation, ShopPlacement } from '@/types/location'

const BASE = '/locations'

/** The parent a list is filtered by: a country for states, a state for cities, a city for groups. */
export interface ListOptions {
  parentId?: string
  includeInactive?: boolean
}

function listQuery(level: LocationLevel, { parentId, includeInactive }: ListOptions): string {
  const params = new URLSearchParams()
  if (parentId) {
    const key = level === 'states' ? 'countryCode' : level === 'cities' ? 'stateId' : level === 'groups' ? 'cityId' : null
    if (key) params.set(key, parentId)
  }
  if (includeInactive) params.set('includeInactive', 'true')
  const text = params.toString()
  return text ? `?${text}` : ''
}

function createBody(level: LocationLevel, parentId: string | undefined, value: NewLocation) {
  switch (level) {
    case 'countries':
      return { isoCode: value.code, name: value.name }
    case 'states':
      return { countryCode: parentId, code: value.code, name: value.name }
    case 'cities':
      return { stateId: Number(parentId), name: value.name }
    case 'groups':
      return { cityId: Number(parentId), name: value.name }
  }
}

export const locationService = {
  list(level: LocationLevel, options: ListOptions = {}): Promise<LocationItem[]> {
    return apiClient.get<LocationItem[]>(`${BASE}/${level}${listQuery(level, options)}`)
  },

  create(level: LocationLevel, parentId: string | undefined, value: NewLocation): Promise<LocationItem> {
    return apiClient.post<LocationItem>(`${BASE}/${level}`, createBody(level, parentId, value))
  },

  /** Renames and switches on or off. Country and state codes are kept as they are. */
  update(level: LocationLevel, item: LocationItem, changes: { name?: string; isActive?: boolean }): Promise<LocationItem> {
    const name = changes.name ?? item.name
    const isActive = changes.isActive ?? item.isActive
    const body = level === 'states' ? { code: item.code, name, isActive } : { name, isActive }
    return apiClient.put<LocationItem>(`${BASE}/${level}/${encodeURIComponent(item.id)}`, body)
  },

  remove(level: LocationLevel, id: string): Promise<{ message: string }> {
    return apiClient.delete<{ message: string }>(`${BASE}/${level}/${encodeURIComponent(id)}`)
  },

  shopsInGroup(groupId: string): Promise<GroupShop[]> {
    return apiClient.get<GroupShop[]>(`${BASE}/groups/${groupId}/shops`)
  },

  /** Places a shop in a city and, optionally, a group of that city. */
  setShopLocation(shopId: string, cityId: number, groupId?: number | null): Promise<ShopPlacement> {
    return apiClient.put<ShopPlacement>(`${BASE}/shops/${shopId}`, { cityId, groupId: groupId ?? null })
  },
}

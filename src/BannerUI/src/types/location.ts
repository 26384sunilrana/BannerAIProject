/** Country > State > City > Group > Shop. Every level has a platform-wide unique identifier. */
export type LocationLevel = 'countries' | 'states' | 'cities' | 'groups'

export interface LocationItem {
  /** Country: the two-letter code. Everything else: a number, sent as text. */
  id: string
  uniqueId: string | null
  name: string
  /** Country and state: the short code. */
  code: string | null
  parentId: string | null
  isActive: boolean
  /** States in a country, cities in a state, groups in a city. */
  childCount: number
  shopCount: number
}

export interface GroupShop {
  id: string
  name: string
  uniqueId: string | null
  status: string
  city: string | null
}

export interface NewLocation {
  name: string
  /** Country: the two-letter code. State: the state code. */
  code?: string
}

export interface ShopPlacement {
  id: string
  uniqueId: string | null
  countryCode: string | null
  stateId: number | null
  cityId: number | null
  groupId: number | null
  city: string | null
}

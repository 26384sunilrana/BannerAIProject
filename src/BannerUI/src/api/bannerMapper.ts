import { Banner, BannerComponent, ComponentData, ComponentType } from '@/types/banner'

/**
 * The editor works with its own component model (type names, x/y/width/height, a `data` object).
 * The API stores numeric types, PositionX/SizeWidth..., and one flat Properties dictionary.
 * These functions are the only place that knows both shapes.
 */

export interface ApiComponent {
  id: string
  bannerId: string
  componentType: number
  positionX: number
  positionY: number
  sizeWidth: number
  sizeHeight: number
  zIndex: number
  properties: Record<string, unknown>
  createdAt: string
}

export interface ApiBannerSummary {
  id: string
  shopId: string
  name: string
  description: string
  width: number
  height: number
  isPublished: boolean
  createdAt: string
  updatedAt: string
}

export interface ApiComponentRequest {
  componentType: number
  positionX: number
  positionY: number
  sizeWidth: number
  sizeHeight: number
  zIndex: number
  properties: Record<string, unknown>
}

export interface ApiBannerRequest {
  name: string
  description: string
  width: number
  height: number
}

const TYPE_TO_API: Record<ComponentType, number> = { text: 1, image: 2, video: 3, graphics: 4 }
const TYPE_FROM_API: Record<number, ComponentType> = { 1: 'text', 2: 'image', 3: 'video', 4: 'graphics' }

// Limits the API enforces; values are brought inside them instead of being rejected
export const LIMITS = { maxSize: 5000, maxZIndex: 100 }

/** Settings that live next to the component's content in the stored properties. */
const APPEARANCE_KEYS = ['rotation', 'opacity', 'isVisible'] as const

const clampInt = (value: number, min: number, max: number) => {
  const n = Number.isFinite(value) ? Math.round(value) : min
  return Math.min(max, Math.max(min, n))
}

export function toApiComponent(component: {
  type: ComponentType
  x: number
  y: number
  width: number
  height: number
  zIndex: number
  rotation?: number
  opacity?: number
  isVisible?: boolean
  data: ComponentData | Record<string, unknown>
}): ApiComponentRequest {
  return {
    componentType: TYPE_TO_API[component.type],
    positionX: clampInt(component.x, 0, Number.MAX_SAFE_INTEGER),
    positionY: clampInt(component.y, 0, Number.MAX_SAFE_INTEGER),
    sizeWidth: clampInt(component.width, 1, LIMITS.maxSize),
    sizeHeight: clampInt(component.height, 1, LIMITS.maxSize),
    zIndex: clampInt(component.zIndex, 0, LIMITS.maxZIndex),
    properties: {
      ...(component.data as Record<string, unknown>),
      rotation: component.rotation ?? 0,
      opacity: component.opacity ?? 1,
      isVisible: component.isVisible ?? true,
    },
  }
}

export function toBannerComponent(api: ApiComponent): BannerComponent {
  const properties = { ...(api.properties ?? {}) }
  const appearance: Record<string, unknown> = {}
  for (const key of APPEARANCE_KEYS) {
    appearance[key] = properties[key]
    delete properties[key]
  }

  return {
    id: api.id,
    bannerId: api.bannerId,
    type: TYPE_FROM_API[api.componentType] ?? 'graphics',
    x: api.positionX,
    y: api.positionY,
    width: api.sizeWidth,
    height: api.sizeHeight,
    zIndex: api.zIndex,
    rotation: typeof appearance.rotation === 'number' ? appearance.rotation : 0,
    opacity: typeof appearance.opacity === 'number' ? appearance.opacity : 1,
    isVisible: typeof appearance.isVisible === 'boolean' ? appearance.isVisible : true,
    data: properties as unknown as ComponentData,
    effects: [],
    createdAt: api.createdAt,
    updatedAt: api.createdAt,
  }
}

export function toBanner(summary: ApiBannerSummary, components: ApiComponent[]): Banner {
  return {
    id: summary.id,
    shopId: summary.shopId,
    title: summary.name,
    description: summary.description ?? '',
    width: summary.width,
    height: summary.height,
    // The API has no background colour yet
    backgroundColor: '#ffffff',
    components: components.map(toBannerComponent).sort((a, b) => a.zIndex - b.zIndex),
    createdAt: summary.createdAt,
    updatedAt: summary.updatedAt,
    version: 1,
  }
}

export function toApiBanner(banner: { title: string; description: string; width: number; height: number }): ApiBannerRequest {
  return {
    name: banner.title.trim().slice(0, 255),
    description: banner.description ?? '',
    width: clampInt(banner.width, 1, LIMITS.maxSize),
    height: clampInt(banner.height, 1, LIMITS.maxSize),
  }
}

/** Lowest unused layer above the existing components, within the API's 0-100 range; null when full. */
export function nextFreeZIndex(zIndexes: number[]): number | null {
  const used = new Set(zIndexes)
  const start = zIndexes.length ? Math.max(...zIndexes) + 1 : 0
  for (let z = start; z <= LIMITS.maxZIndex; z++) if (!used.has(z)) return z
  for (let z = 0; z < start; z++) if (!used.has(z)) return z
  return null
}

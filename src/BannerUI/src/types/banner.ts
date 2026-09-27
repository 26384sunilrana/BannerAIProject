export type ComponentType = 'text' | 'image' | 'video' | 'graphics'

export type EffectType = 'opacity' | 'rotation' | 'scale' | 'blur' | 'animation'

export type TransitionType = 'fade' | 'slide' | 'zoom' | 'flip'

export interface Banner {
  id: string
  shopId: string
  title: string
  description: string
  width: number
  height: number
  backgroundColor: string
  components: BannerComponent[]
  createdAt: string
  updatedAt: string
  version: number
}

export interface BannerComponent {
  id: string
  bannerId: string
  type: ComponentType
  x: number
  y: number
  width: number
  height: number
  zIndex: number
  rotation: number
  opacity: number
  isVisible: boolean
  data: ComponentData
  effects: Effect[]
  carouselId?: string
  createdAt: string
  updatedAt: string
}

export type ComponentData = TextData | ImageData | VideoData | GraphicsData

export interface TextData {
  content: string
  fontSize: number
  fontFamily: string
  fontWeight: string
  color: string
  textAlign: 'left' | 'center' | 'right'
  lineHeight: number
}

export interface ImageData {
  mediaFileId: string
  mediaUrl: string
  alt: string
  objectFit: 'cover' | 'contain' | 'fill'
}

export interface VideoData {
  mediaFileId: string
  mediaUrl: string
  poster: string
  autoPlay: boolean
  loop: boolean
  muted: boolean
  duration: number
  resolution: string
}

export interface GraphicsData {
  shapeType: 'circle' | 'rectangle' | 'triangle'
  fillColor: string
  strokeColor: string
  strokeWidth: number
}

export interface Effect {
  id: string
  componentId: string
  type: EffectType
  parameters: Record<string, unknown>
  enabled: boolean
}

export interface Carousel {
  id: string
  bannerId: string
  intervalMs: number
  transitionDuration: number
  transitionType: TransitionType
  components: BannerComponent[]
  createdAt: string
  updatedAt: string
}

export interface BannerVersion {
  id: string
  bannerId: string
  versionNumber: number
  snapshot: BannerSnapshot
  createdAt: string
  status: 'pending' | 'active' | 'failed'
}

export interface BannerSnapshot {
  bannerId: string
  components: ComponentSnapshot[]
  metadata: Record<string, unknown>
}

export interface ComponentSnapshot {
  id: string
  type: ComponentType
  x: number
  y: number
  width: number
  height: number
  zIndex: number
  data: ComponentData
}

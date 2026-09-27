import { BannerComponent, Banner } from './banner'

export interface EditorState {
  banner: Banner | null
  components: BannerComponent[]
  selectedComponentId: string | null
  isDirty: boolean
  isLoading: boolean
  isSaving: boolean
  error: string | null
  canvasWidth: number
  canvasHeight: number
  scale: number
  isPreviewMode: boolean
  history: EditorHistoryEntry[]
  historyIndex: number
}

export interface EditorHistoryEntry {
  state: BannerComponent[]
  timestamp: number
  description: string
}

export type EditorAction =
  | { type: 'SET_BANNER'; payload: Banner }
  | { type: 'SELECT_COMPONENT'; payload: string | null }
  | { type: 'ADD_COMPONENT'; payload: BannerComponent }
  | { type: 'UPDATE_COMPONENT'; payload: { id: string; updates: Partial<BannerComponent> } }
  | { type: 'DELETE_COMPONENT'; payload: string }
  | { type: 'SET_COMPONENTS'; payload: BannerComponent[] }
  | { type: 'SET_DIRTY'; payload: boolean }
  | { type: 'SET_LOADING'; payload: boolean }
  | { type: 'SET_SAVING'; payload: boolean }
  | { type: 'SET_ERROR'; payload: string | null }
  | { type: 'SET_SCALE'; payload: number }
  | { type: 'SET_PREVIEW_MODE'; payload: boolean }
  | { type: 'PUSH_HISTORY'; payload: EditorHistoryEntry }
  | { type: 'UNDO' }
  | { type: 'REDO' }

export interface PlacedComponent extends BannerComponent {
  isDragging?: boolean
  isResizing?: boolean
}

export interface DragState {
  isDragging: boolean
  dragStartX: number
  dragStartY: number
  dragStartComponentX: number
  dragStartComponentY: number
}

export interface ResizeState {
  isResizing: boolean
  resizeStartX: number
  resizeStartY: number
  resizeStartWidth: number
  resizeStartHeight: number
}

export interface SelectionState {
  selectedComponentId: string | null
  multiSelect: string[]
}

export interface CanvasState {
  scale: number
  offsetX: number
  offsetY: number
  width: number
  height: number
}

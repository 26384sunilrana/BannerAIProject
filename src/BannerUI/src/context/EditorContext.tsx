'use client'

import React, { createContext, useReducer, useCallback, ReactNode } from 'react'
import { EditorState, EditorAction, EditorHistoryEntry } from '@/types/editor'
import { Banner, BannerComponent } from '@/types/banner'

export interface EditorContextType {
  state: EditorState
  selectComponent: (id: string | null) => void
  addComponent: (component: BannerComponent) => void
  updateComponent: (id: string, updates: Partial<BannerComponent>) => void
  deleteComponent: (id: string) => void
  setBanner: (banner: Banner) => void
  setDirty: (dirty: boolean) => void
  setLoading: (loading: boolean) => void
  setSaving: (saving: boolean) => void
  setError: (error: string | null) => void
  setScale: (scale: number) => void
  setPreviewMode: (preview: boolean) => void
  undo: () => void
  redo: () => void
}

const initialState: EditorState = {
  banner: null,
  components: [],
  selectedComponentId: null,
  isDirty: false,
  isLoading: true,
  isSaving: false,
  error: null,
  canvasWidth: parseInt(process.env.NEXT_PUBLIC_CANVAS_WIDTH || '1200'),
  canvasHeight: parseInt(process.env.NEXT_PUBLIC_CANVAS_HEIGHT || '600'),
  scale: 1,
  isPreviewMode: false,
  history: [],
  historyIndex: -1,
}

function editorReducer(state: EditorState, action: EditorAction): EditorState {
  switch (action.type) {
    case 'SET_BANNER':
      return {
        ...state,
        banner: action.payload,
        components: action.payload.components,
        isLoading: false,
      }

    case 'SELECT_COMPONENT':
      return {
        ...state,
        selectedComponentId: action.payload,
      }

    case 'ADD_COMPONENT':
      return {
        ...state,
        components: [...state.components, action.payload],
        isDirty: true,
      }

    case 'UPDATE_COMPONENT':
      return {
        ...state,
        components: state.components.map((c) =>
          c.id === action.payload.id ? { ...c, ...action.payload.updates } : c
        ),
        isDirty: true,
      }

    case 'DELETE_COMPONENT':
      return {
        ...state,
        components: state.components.filter((c) => c.id !== action.payload),
        selectedComponentId:
          state.selectedComponentId === action.payload ? null : state.selectedComponentId,
        isDirty: true,
      }

    case 'SET_COMPONENTS':
      return {
        ...state,
        components: action.payload,
      }

    case 'SET_DIRTY':
      return {
        ...state,
        isDirty: action.payload,
      }

    case 'SET_LOADING':
      return {
        ...state,
        isLoading: action.payload,
      }

    case 'SET_SAVING':
      return {
        ...state,
        isSaving: action.payload,
      }

    case 'SET_ERROR':
      return {
        ...state,
        error: action.payload,
      }

    case 'SET_SCALE':
      return {
        ...state,
        scale: action.payload,
      }

    case 'SET_PREVIEW_MODE':
      return {
        ...state,
        isPreviewMode: action.payload,
      }

    case 'PUSH_HISTORY':
      return {
        ...state,
        history: [
          ...state.history.slice(0, state.historyIndex + 1),
          action.payload,
        ],
        historyIndex: state.history.length,
      }

    case 'UNDO':
      if (state.historyIndex <= 0) return state
      const previousEntry = state.history[state.historyIndex - 1]
      return {
        ...state,
        components: previousEntry.state,
        historyIndex: state.historyIndex - 1,
      }

    case 'REDO':
      if (state.historyIndex >= state.history.length - 1) return state
      const nextEntry = state.history[state.historyIndex + 1]
      return {
        ...state,
        components: nextEntry.state,
        historyIndex: state.historyIndex + 1,
      }

    default:
      return state
  }
}

export const EditorContext = createContext<EditorContextType | undefined>(undefined)

interface EditorProviderProps {
  children: ReactNode
}

export function EditorProvider({ children }: EditorProviderProps) {
  const [state, dispatch] = useReducer(editorReducer, initialState)

  const selectComponent = useCallback((id: string | null) => {
    dispatch({ type: 'SELECT_COMPONENT', payload: id })
  }, [])

  const addComponent = useCallback((component: BannerComponent) => {
    dispatch({ type: 'ADD_COMPONENT', payload: component })
  }, [])

  const updateComponent = useCallback(
    (id: string, updates: Partial<BannerComponent>) => {
      dispatch({ type: 'UPDATE_COMPONENT', payload: { id, updates } })
    },
    []
  )

  const deleteComponent = useCallback((id: string) => {
    dispatch({ type: 'DELETE_COMPONENT', payload: id })
  }, [])

  const setBanner = useCallback((banner: Banner) => {
    dispatch({ type: 'SET_BANNER', payload: banner })
  }, [])

  const setDirty = useCallback((dirty: boolean) => {
    dispatch({ type: 'SET_DIRTY', payload: dirty })
  }, [])

  const setLoading = useCallback((loading: boolean) => {
    dispatch({ type: 'SET_LOADING', payload: loading })
  }, [])

  const setSaving = useCallback((saving: boolean) => {
    dispatch({ type: 'SET_SAVING', payload: saving })
  }, [])

  const setError = useCallback((error: string | null) => {
    dispatch({ type: 'SET_ERROR', payload: error })
  }, [])

  const setScale = useCallback((scale: number) => {
    dispatch({ type: 'SET_SCALE', payload: scale })
  }, [])

  const setPreviewMode = useCallback((preview: boolean) => {
    dispatch({ type: 'SET_PREVIEW_MODE', payload: preview })
  }, [])

  const undo = useCallback(() => {
    dispatch({ type: 'UNDO' })
  }, [])

  const redo = useCallback(() => {
    dispatch({ type: 'REDO' })
  }, [])

  const value: EditorContextType = {
    state,
    selectComponent,
    addComponent,
    updateComponent,
    deleteComponent,
    setBanner,
    setDirty,
    setLoading,
    setSaving,
    setError,
    setScale,
    setPreviewMode,
    undo,
    redo,
  }

  return <EditorContext.Provider value={value}>{children}</EditorContext.Provider>
}

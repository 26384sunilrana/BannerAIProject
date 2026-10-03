'use client'

import React, { createContext, useReducer, useCallback, ReactNode } from 'react'
import { EditorState, EditorAction } from '@/types/editor'
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
  past: [],
  future: [],
  lastEdit: null,
}

/** Edits to the same component closer together than this are one undo step. */
export const UNDO_MERGE_MS = 700
export const MAX_UNDO_STEPS = 50

const noHistory = { past: [], future: [], lastEdit: null }

function editorReducer(state: EditorState, action: EditorAction): EditorState {
  switch (action.type) {
    case 'SET_BANNER':
      return {
        ...state,
        banner: action.payload,
        components: action.payload.components,
        isLoading: false,
        ...noHistory,
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
        // added on the server straight away, so earlier states no longer match what is stored
        ...noHistory,
      }

    case 'UPDATE_COMPONENT': {
      const at = action.payload.at ?? Date.now()
      const merge = state.lastEdit !== null && state.lastEdit.id === action.payload.id && at - state.lastEdit.at < UNDO_MERGE_MS
      return {
        ...state,
        components: state.components.map((c) =>
          c.id === action.payload.id ? { ...c, ...action.payload.updates } : c
        ),
        isDirty: true,
        past: merge ? state.past : [...state.past, state.components].slice(-MAX_UNDO_STEPS),
        future: [],
        lastEdit: { id: action.payload.id, at },
      }
    }

    case 'DELETE_COMPONENT':
      return {
        ...state,
        components: state.components.filter((c) => c.id !== action.payload),
        selectedComponentId:
          state.selectedComponentId === action.payload ? null : state.selectedComponentId,
        isDirty: true,
        ...noHistory,
      }

    case 'SET_COMPONENTS':
      return {
        ...state,
        components: action.payload,
        ...noHistory,
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

    case 'UNDO': {
      if (state.past.length === 0) return state
      const previous = state.past[state.past.length - 1]
      return {
        ...state,
        components: previous,
        past: state.past.slice(0, -1),
        future: [state.components, ...state.future],
        lastEdit: null,
        isDirty: true,
      }
    }

    case 'REDO': {
      if (state.future.length === 0) return state
      const [next, ...rest] = state.future
      return {
        ...state,
        components: next,
        past: [...state.past, state.components].slice(-MAX_UNDO_STEPS),
        future: rest,
        lastEdit: null,
        isDirty: true,
      }
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
      dispatch({ type: 'UPDATE_COMPONENT', payload: { id, updates, at: Date.now() } })
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

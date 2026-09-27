'use client'

import { useContext } from 'react'
import { EditorContext, EditorContextType } from '@/context/EditorContext'

export function useEditor(): EditorContextType {
  const context = useContext(EditorContext)
  if (!context) {
    throw new Error('useEditor must be used within EditorProvider')
  }
  return context
}

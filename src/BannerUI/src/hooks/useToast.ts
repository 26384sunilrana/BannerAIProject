'use client'

import { useCallback, useState } from 'react'
import { ToastMessage, ToastType } from '@/components/Common'

export function useToast() {
  const [messages, setMessages] = useState<ToastMessage[]>([])

  const show = useCallback((message: string, type: ToastType = 'info', duration = 5000) => {
    const id = `${Date.now()}-${Math.random()}`
    const toast: ToastMessage = { id, message, type, duration }

    setMessages((prev) => [...prev, toast])

    if (duration > 0) {
      setTimeout(() => {
        remove(id)
      }, duration)
    }

    return id
  }, [])

  const remove = useCallback((id: string) => {
    setMessages((prev) => prev.filter((m) => m.id !== id))
  }, [])

  const success = useCallback((message: string, duration?: number) => {
    return show(message, 'success', duration)
  }, [show])

  const error = useCallback((message: string, duration?: number) => {
    return show(message, 'error', duration || 7000)
  }, [show])

  const warning = useCallback((message: string, duration?: number) => {
    return show(message, 'warning', duration)
  }, [show])

  const info = useCallback((message: string, duration?: number) => {
    return show(message, 'info', duration)
  }, [show])

  return {
    messages,
    show,
    remove,
    success,
    error,
    warning,
    info,
  }
}

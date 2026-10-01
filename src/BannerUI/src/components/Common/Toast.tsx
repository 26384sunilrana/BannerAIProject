'use client'

import React, { useEffect } from 'react'

export type ToastType = 'success' | 'error' | 'warning' | 'info'

export interface ToastMessage {
  id: string
  type: ToastType
  message: string
  duration?: number
}

export interface ToastProps {
  messages: ToastMessage[]
  onRemove: (id: string) => void
}

const typeStyles = {
  success: 'bg-green-50 text-green-800 border-green-200',
  error: 'bg-red-50 text-red-800 border-red-200',
  warning: 'bg-yellow-50 text-yellow-800 border-yellow-200',
  info: 'bg-blue-50 text-blue-800 border-blue-200',
}

const typeIcons = {
  success: '✓',
  error: '✕',
  warning: '⚠',
  info: 'ℹ',
}

function ToastItem({ message, onRemove }: { message: ToastMessage; onRemove: () => void }) {
  useEffect(() => {
    const timer = setTimeout(onRemove, message.duration || 5000)
    return () => clearTimeout(timer)
  }, [message, onRemove])

  return (
    <div
      className={`
        flex items-center gap-3 p-4 rounded-lg border-l-4
        animate-in fade-in slide-in-from-top
        ${typeStyles[message.type]}
      `}
      role="alert"
    >
      <span className="text-lg font-bold">{typeIcons[message.type]}</span>
      <span className="flex-1">{message.message}</span>
      <button
        onClick={onRemove}
        className="text-lg hover:opacity-70 transition-opacity"
        aria-label="Close"
      >
        ✕
      </button>
    </div>
  )
}

export function Toast({ messages, onRemove }: ToastProps) {
  return (
    <div className="fixed top-4 right-4 z-50 flex flex-col gap-2 max-w-md pointer-events-auto">
      {messages.map((message) => (
        <ToastItem key={message.id} message={message} onRemove={() => onRemove(message.id)} />
      ))}
    </div>
  )
}

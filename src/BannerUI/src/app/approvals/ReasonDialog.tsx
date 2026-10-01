'use client'

import React, { FormEvent, useEffect, useState } from 'react'
import { Button } from '@/components/Common'

interface ReasonDialogProps {
  isOpen: boolean
  title: string
  label: string
  confirmText: string
  onConfirm: (reason: string) => void | Promise<void>
  onCancel: () => void
}

/** Asks for a required piece of text (such as a rejection reason) before continuing. */
export function ReasonDialog({ isOpen, title, label, confirmText, onConfirm, onCancel }: ReasonDialogProps) {
  const [reason, setReason] = useState('')
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    if (isOpen) {
      setReason('')
      setError(null)
    }
  }, [isOpen])

  if (!isOpen) return null

  const submit = (event: FormEvent) => {
    event.preventDefault()
    if (!reason.trim()) {
      setError('Please give a reason.')
      return
    }
    onConfirm(reason.trim())
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4" role="dialog" aria-modal="true" aria-labelledby="reason-title">
      <form onSubmit={submit} className="w-full max-w-md rounded-xl bg-white p-6 shadow-xl">
        <h2 id="reason-title" className="text-lg font-semibold text-gray-900">
          {title}
        </h2>
        <label htmlFor="reason" className="mt-4 block text-sm font-medium text-gray-700">
          {label}
        </label>
        <textarea
          id="reason"
          autoFocus
          rows={3}
          value={reason}
          onChange={(e) => setReason(e.target.value)}
          className="mt-1 w-full rounded-lg border-2 border-gray-300 px-3 py-2 focus:border-blue-500 focus-visible:outline-none"
        />
        {error && (
          <p role="alert" className="mt-1 text-sm text-red-600">
            {error}
          </p>
        )}
        <div className="mt-5 flex justify-end gap-2">
          <Button type="button" variant="secondary" onClick={onCancel}>
            Cancel
          </Button>
          <Button type="submit" variant="danger">
            {confirmText}
          </Button>
        </div>
      </form>
    </div>
  )
}

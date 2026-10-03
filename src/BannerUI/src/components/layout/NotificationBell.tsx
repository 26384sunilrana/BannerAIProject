'use client'

import React, { useCallback, useEffect, useRef, useState } from 'react'
import Link from 'next/link'
import { notificationService, type AppNotification } from '@/api/notificationService'

const POLL_MS = 60_000

/** The bell in the top bar: how many messages are unread, and the latest ones. Quietly shows nothing new when the server cannot be reached. */
export function NotificationBell() {
  const [unread, setUnread] = useState(0)
  const [open, setOpen] = useState(false)
  const [items, setItems] = useState<AppNotification[] | null>(null)
  const [failed, setFailed] = useState(false)
  const box = useRef<HTMLDivElement>(null)

  const refreshCount = useCallback(() => {
    if (typeof document !== 'undefined' && document.hidden) return
    notificationService.unreadCount().then(setUnread).catch(() => undefined)
  }, [])

  useEffect(() => {
    refreshCount()
    const timer = setInterval(refreshCount, POLL_MS)
    document.addEventListener('visibilitychange', refreshCount)
    return () => {
      clearInterval(timer)
      document.removeEventListener('visibilitychange', refreshCount)
    }
  }, [refreshCount])

  useEffect(() => {
    if (!open) return
    const away = (event: MouseEvent) => !box.current?.contains(event.target as Node) && setOpen(false)
    const escape = (event: KeyboardEvent) => event.key === 'Escape' && setOpen(false)
    document.addEventListener('mousedown', away)
    document.addEventListener('keydown', escape)
    return () => {
      document.removeEventListener('mousedown', away)
      document.removeEventListener('keydown', escape)
    }
  }, [open])

  const toggle = async () => {
    const next = !open
    setOpen(next)
    if (!next) return
    setFailed(false)
    try {
      const page = await notificationService.list(1, 10)
      setItems(page.items)
      setUnread(page.unread)
    } catch {
      setFailed(true)
    }
  }

  const read = (item: AppNotification) => {
    setOpen(false)
    if (item.isRead) return
    setItems((all) => all?.map((n) => (n.id === item.id ? { ...n, isRead: true } : n)) ?? null)
    notificationService.markRead(item.id).then((r) => setUnread(r.unread)).catch(() => undefined)
  }

  const readAll = async () => {
    try {
      await notificationService.markAllRead()
      setUnread(0)
      setItems((all) => all?.map((n) => ({ ...n, isRead: true })) ?? null)
    } catch {
      setFailed(true)
    }
  }

  return (
    <div ref={box} className="relative">
      <button
        type="button"
        onClick={toggle}
        aria-haspopup="true"
        aria-expanded={open}
        aria-label={unread > 0 ? `Messages, ${unread} unread` : 'Messages'}
        className="relative rounded-full p-1.5 text-gray-600 hover:bg-gray-100 hover:text-gray-900"
      >
        <svg aria-hidden="true" viewBox="0 0 24 24" className="h-5 w-5" fill="none" stroke="currentColor" strokeWidth="2">
          <path d="M18 8a6 6 0 10-12 0c0 7-3 9-3 9h18s-3-2-3-9M13.7 21a2 2 0 01-3.4 0" strokeLinecap="round" strokeLinejoin="round" />
        </svg>
        {unread > 0 && (
          <span data-testid="bell-count" className="absolute -right-1 -top-1 min-w-[1.1rem] rounded-full bg-red-600 px-1 text-center text-[0.65rem] font-semibold leading-[1.1rem] text-white">
            {unread > 99 ? '99+' : unread}
          </span>
        )}
      </button>

      {open && (
        <div role="region" aria-label="Messages" className="absolute right-0 z-20 mt-2 w-80 max-w-[90vw] rounded-xl border border-gray-200 bg-white shadow-lg">
          <div className="flex items-center justify-between border-b border-gray-100 px-4 py-2">
            <span className="font-semibold text-gray-900">Messages</span>
            {unread > 0 && (
              <button type="button" onClick={readAll} className="text-xs font-medium text-blue-700 hover:underline">
                Mark all as read
              </button>
            )}
          </div>
          {failed && <p role="alert" className="px-4 py-3 text-sm text-red-700">Messages could not be loaded.</p>}
          {!failed && items === null && <p className="px-4 py-3 text-sm text-gray-500">Loading…</p>}
          {!failed && items?.length === 0 && <p className="px-4 py-6 text-center text-sm text-gray-500">No messages yet.</p>}
          <ul className="max-h-96 divide-y divide-gray-100 overflow-y-auto">
            {items?.map((item) => {
              const body = (
                <>
                  <span className="flex items-center gap-2">
                    {!item.isRead && <span aria-label="Unread" className="h-2 w-2 shrink-0 rounded-full bg-blue-600" />}
                    <span className={item.isRead ? 'text-gray-700' : 'font-semibold text-gray-900'}>{item.title}</span>
                  </span>
                  <span className="mt-0.5 block text-sm text-gray-600">{item.message}</span>
                  <span className="mt-0.5 block text-xs text-gray-400">{new Date(item.createdAt).toLocaleString()}</span>
                </>
              )
              return (
                <li key={item.id} data-testid={`message-${item.kind}`}>
                  {item.linkUrl ? (
                    <Link href={item.linkUrl} onClick={() => read(item)} className="block px-4 py-2.5 hover:bg-gray-50">
                      {body}
                    </Link>
                  ) : (
                    <button type="button" onClick={() => read(item)} className="block w-full px-4 py-2.5 text-left hover:bg-gray-50">
                      {body}
                    </button>
                  )}
                </li>
              )
            })}
          </ul>
        </div>
      )}
    </div>
  )
}

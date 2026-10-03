'use client'

import React, { useEffect, useState } from 'react'

export interface BoardLook {
  message?: string | null
  /** #rrggbb; the standard dark blue when empty. */
  background?: string | null
  textColor?: string | null
  /** A link or data address of the shop's logo. */
  logoUrl?: string | null
}

interface DefaultBoardProps {
  shopName: string
  /** Optional explanation shown small at the bottom, for example that a plan has ended. */
  note?: string
  /** The shop's own design; the standard board when left out. */
  look?: BoardLook | null
  /** Draws the clock as a still picture, for the designer preview. */
  frozenTime?: Date
}

/** The shop's own default board: always available on the machine, no server needed. */
export function DefaultBoard({ shopName, note, look, frozenTime }: DefaultBoardProps) {
  const [now, setNow] = useState(() => frozenTime ?? new Date())

  useEffect(() => {
    if (frozenTime) {
      setNow(frozenTime)
      return
    }
    const timer = setInterval(() => setNow(new Date()), 1000)
    return () => clearInterval(timer)
  }, [frozenTime])

  const custom = !!look?.background
  const text = look?.textColor || '#ffffff'
  const soft = { color: text, opacity: 0.8 }

  return (
    <div
      data-testid="default-board"
      className={`relative flex h-full w-full flex-col items-center justify-center text-white ${custom ? '' : 'bg-gradient-to-br from-slate-900 via-blue-900 to-slate-800'}`}
      style={{ ...(custom ? { background: look!.background! } : {}), color: text }}
    >
      {look?.logoUrl && (
        // eslint-disable-next-line @next/next/no-img-element
        <img src={look.logoUrl} alt="" data-testid="default-board-logo" style={{ maxHeight: '28%', maxWidth: '50%', objectFit: 'contain', marginBottom: '1.5rem' }} />
      )}
      <p className="text-sm uppercase tracking-[0.3em]" style={custom ? soft : { color: '#bfdbfe' }}>
        Welcome to
      </p>
      <h1 className="mt-4 px-8 text-center text-6xl font-bold">{shopName || 'our shop'}</h1>
      {look?.message && (
        <p data-testid="default-board-message" className="mt-6 max-w-4xl whitespace-pre-line px-8 text-center text-3xl" style={custom ? { color: text } : { color: '#e0f2fe' }}>
          {look.message}
        </p>
      )}
      <p className="mt-10 text-3xl tabular-nums" style={custom ? soft : { color: '#dbeafe' }} aria-label="Current time">
        {now.toLocaleTimeString(undefined, { hour: 'numeric', minute: '2-digit' })}
      </p>
      <p className="text-lg" style={custom ? soft : { color: '#bfdbfe' }}>
        {now.toLocaleDateString(undefined, { weekday: 'long', day: 'numeric', month: 'long' })}
      </p>
      {note && (
        <p className="absolute bottom-6 px-6 text-center text-sm" style={custom ? soft : { color: '#93c5fd' }}>
          {note}
        </p>
      )}
    </div>
  )
}

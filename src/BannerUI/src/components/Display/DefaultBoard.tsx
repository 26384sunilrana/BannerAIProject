'use client'

import React, { useEffect, useState } from 'react'

interface DefaultBoardProps {
  shopName: string
  /** Optional explanation shown small at the bottom, for example that a plan has ended. */
  note?: string
}

/** The shop's own default board: always available on the machine, no server needed. */
export function DefaultBoard({ shopName, note }: DefaultBoardProps) {
  const [now, setNow] = useState(() => new Date())

  useEffect(() => {
    const timer = setInterval(() => setNow(new Date()), 1000)
    return () => clearInterval(timer)
  }, [])

  return (
    <div
      data-testid="default-board"
      className="flex h-full w-full flex-col items-center justify-center bg-gradient-to-br from-slate-900 via-blue-900 to-slate-800 text-white"
    >
      <p className="text-sm uppercase tracking-[0.3em] text-blue-200">Welcome to</p>
      <h1 className="mt-4 px-8 text-center text-6xl font-bold">{shopName || 'our shop'}</h1>
      <p className="mt-10 text-3xl tabular-nums text-blue-100" aria-label="Current time">
        {now.toLocaleTimeString(undefined, { hour: 'numeric', minute: '2-digit' })}
      </p>
      <p className="text-lg text-blue-200">{now.toLocaleDateString(undefined, { weekday: 'long', day: 'numeric', month: 'long' })}</p>
      {note && <p className="absolute bottom-6 px-6 text-center text-sm text-blue-300">{note}</p>}
    </div>
  )
}

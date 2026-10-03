'use client'

import React, { FormEvent, useEffect, useRef, useState } from 'react'
import { AppShell } from '@/components/layout/AppShell'
import { Button, Input, Toast } from '@/components/Common'
import { DefaultBoard } from '@/components/Display/DefaultBoard'
import { MediaPickerDialog } from '@/components/media/MediaPickerDialog'
import { useAuth } from '@/context/AuthContext'
import { useToast } from '@/hooks/useToast'
import { defaultBoardService, DefaultBoardSettings } from '@/api/defaultBoardService'
import { MediaLibraryItem } from '@/api/mediaService'
import { getErrorMessage } from '@/api/client'
import { Roles } from '@/lib/session'

const STANDARD_TEXT = '#ffffff'
const STANDARD_BACKGROUND = '#1e3a8a'
const PREVIEW_TIME = new Date(2030, 0, 7, 10, 30)

export default function DefaultBoardPage() {
  return (
    <AppShell roles={[Roles.ShopOwner]}>
      <Designer />
    </AppShell>
  )
}

function Designer() {
  const { user } = useAuth()
  const shopId = user?.shopId ?? null
  const toast = useToast()

  const [saved, setSaved] = useState<DefaultBoardSettings | null>(null)
  const [message, setMessage] = useState('')
  const [useColours, setUseColours] = useState(false)
  const [background, setBackground] = useState(STANDARD_BACKGROUND)
  const [textColor, setTextColor] = useState(STANDARD_TEXT)
  const [logo, setLogo] = useState<{ id: string; url: string } | null>(null)
  const [picking, setPicking] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [loadError, setLoadError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)
  const holder = useRef<HTMLDivElement>(null)
  const [scale, setScale] = useState(0.4)

  // the preview is drawn at the size of a real screen and shrunk to fit the space
  useEffect(() => {
    const element = holder.current
    if (!element) return
    const fit = () => element.clientWidth > 0 && setScale(element.clientWidth / 1280)
    fit()
    if (typeof ResizeObserver === 'undefined') return
    const observer = new ResizeObserver(fit)
    observer.observe(element)
    return () => observer.disconnect()
  })

  const apply = (board: DefaultBoardSettings) => {
    setSaved(board)
    setMessage(board.message ?? '')
    setUseColours(!!board.background)
    setBackground(board.background ?? STANDARD_BACKGROUND)
    setTextColor(board.textColor ?? STANDARD_TEXT)
    setLogo(board.logoMediaFileId && board.logoUrl ? { id: board.logoMediaFileId, url: board.logoUrl } : null)
  }

  useEffect(() => {
    if (!shopId) return
    defaultBoardService
      .get(shopId)
      .then(apply)
      .catch((err) => setLoadError(getErrorMessage(err, 'Could not load your default board.')))
  }, [shopId])

  if (!shopId) return <p className="text-gray-600">Your account is not linked to a shop.</p>
  if (loadError) return <p role="alert" className="text-red-700">{loadError}</p>
  if (!saved) return <p className="text-gray-500">Loading your default board…</p>

  const save = async (event: FormEvent) => {
    event.preventDefault()
    setBusy(true)
    setError(null)
    try {
      apply(
        await defaultBoardService.save(shopId, {
          message: message.trim() || null,
          background: useColours ? background : null,
          textColor: useColours ? textColor : null,
          logoMediaFileId: logo?.id ?? null,
        })
      )
      toast.success('Your default board was saved. Your screen picks it up within a minute.')
    } catch (err) {
      setError(getErrorMessage(err, 'The default board could not be saved.'))
    } finally {
      setBusy(false)
    }
  }

  const pickLogo = (item: MediaLibraryItem) => {
    setPicking(false)
    setLogo({ id: item.id, url: item.url })
  }

  return (
    <div className="space-y-6">
      <Toast messages={toast.messages} onRemove={toast.remove} />

      <header>
        <h1 className="text-2xl font-semibold text-gray-900">Default board</h1>
        <p className="mt-1 text-gray-600">
          What your screen shows when no banner is live: between banners, before the first one starts, or if your plan has ended. It is kept on
          the screen&apos;s own machine, so it shows even without a connection.
        </p>
      </header>

      <div className="grid gap-6 lg:grid-cols-[minmax(0,26rem)_minmax(0,1fr)]">
        <form onSubmit={save} noValidate className="space-y-4 rounded-xl border border-gray-200 bg-white p-5">
          {error && (
            <div role="alert" className="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-800">
              {error}
            </div>
          )}

          <div>
            <label htmlFor="board-message" className="text-sm font-medium text-gray-700">
              Message
            </label>
            <textarea
              id="board-message"
              rows={3}
              maxLength={200}
              value={message}
              onChange={(e) => setMessage(e.target.value)}
              placeholder="For example: Open 9 to 9, every day"
              className="mt-1 w-full rounded-lg border-2 border-gray-300 px-3 py-2 text-gray-900 focus-visible:outline-none focus:border-blue-500"
            />
            <p className="text-xs text-gray-500">{message.length} of 200 characters</p>
          </div>

          <div className="space-y-2">
            <p className="text-sm font-medium text-gray-700">Logo</p>
            {logo ? (
              <div className="flex items-center gap-3">
                {/* eslint-disable-next-line @next/next/no-img-element */}
                <img src={logo.url} alt="Your logo" className="h-12 max-w-[8rem] rounded border border-gray-200 object-contain" />
                <Button type="button" size="sm" variant="ghost" onClick={() => setLogo(null)}>
                  Remove logo
                </Button>
              </div>
            ) : (
              <p className="text-sm text-gray-500">No logo yet.</p>
            )}
            <Button type="button" size="sm" variant="secondary" onClick={() => setPicking(true)}>
              {logo ? 'Choose another logo' : 'Choose a logo from my files'}
            </Button>
          </div>

          <div className="space-y-2">
            <label className="flex items-center gap-2 text-sm text-gray-800">
              <input type="checkbox" checked={useColours} onChange={(e) => setUseColours(e.target.checked)} />
              Use my own colours
            </label>
            {useColours && (
              <div className="grid grid-cols-2 gap-3">
                <Input id="board-background" type="color" label="Background" value={background} onChange={(e) => setBackground(e.target.value)} />
                <Input id="board-text" type="color" label="Text" value={textColor} onChange={(e) => setTextColor(e.target.value)} />
              </div>
            )}
          </div>

          <Button type="submit" isLoading={busy}>
            Save default board
          </Button>
        </form>

        <section aria-label="Preview">
          <p className="mb-2 text-sm font-medium text-gray-700">Preview</p>
          <div ref={holder} className="aspect-video w-full overflow-hidden rounded-xl border border-gray-200 bg-black" data-testid="board-preview">
            <div style={{ width: '1280px', height: '720px', transform: `scale(${scale})`, transformOrigin: 'top left' }}>
              <DefaultBoard
                shopName={saved.shopName}
                frozenTime={PREVIEW_TIME}
                look={{ message: message.trim() || null, background: useColours ? background : null, textColor: useColours ? textColor : null, logoUrl: logo?.url ?? null }}
              />
            </div>
          </div>
        </section>
      </div>

      {picking && <MediaPickerDialog kind="image" onSelect={pickLogo} onClose={() => setPicking(false)} />}
    </div>
  )
}

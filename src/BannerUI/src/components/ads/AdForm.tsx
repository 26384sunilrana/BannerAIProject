'use client'

import React, { FormEvent, useState } from 'react'
import { Button, Input, Select } from '@/components/Common'
import { AdTile } from '@/components/Display/AdLayout'
import { MediaPickerDialog } from '@/components/media/MediaPickerDialog'
import { MediaLibraryItem } from '@/api/mediaService'
import { AdInput, AdKind, AdPlacement, ShopAd } from '@/api/adService'
import { ALL_DAYS, WEEKDAYS, fromMinutes, toMinutes, utcToWallTime, wallTimeToUtcIso, zoneLabel } from '@/lib/timeZones'

const KINDS: { value: AdKind; label: string; help: string }[] = [
  { value: 'Side', label: 'Side strip', help: 'A strip along one edge. The banner keeps the rest of the screen.' },
  { value: 'Mega', label: 'Mega ad', help: 'Takes most of the screen for a period. The banner moves to the smaller part.' },
  { value: 'Popup', label: 'Popup', help: 'Appears over the banner for a few seconds, now and then.' },
  { value: 'Minor', label: 'Small corner tile', help: 'A small tile in one corner of the screen.' },
]

const EDGES: { value: AdPlacement; label: string }[] = [
  { value: 'Left', label: 'Left' },
  { value: 'Right', label: 'Right' },
  { value: 'Top', label: 'Top' },
  { value: 'Bottom', label: 'Bottom' },
]

const CORNERS: { value: AdPlacement; label: string }[] = [
  { value: 'TopLeft', label: 'Top left' },
  { value: 'TopRight', label: 'Top right' },
  { value: 'BottomLeft', label: 'Bottom left' },
  { value: 'BottomRight', label: 'Bottom right' },
]

const DAY_CHOICES = [
  { value: String(ALL_DAYS), label: 'Every day' },
  { value: String(WEEKDAYS), label: 'Monday to Friday' },
  { value: String(1 | 64), label: 'Weekends' },
]

interface Props {
  /** The ad being changed; leave out to book a new one. */
  ad?: ShopAd
  /** The clock the dates are read on: the shop's time zone. */
  zone: string
  /** Administrators cannot attach pictures: the files belong to the shop. */
  allowPicture: boolean
  busy: boolean
  error: string | null
  onSubmit: (input: AdInput) => void
  onCancel: () => void
}

/** The form for booking an ad. The preview on the right is the ad as it will look. */
export function AdForm({ ad, zone, allowPicture, busy, error, onSubmit, onCancel }: Props) {
  const [advertiser, setAdvertiser] = useState(ad?.advertiserName ?? '')
  const [headline, setHeadline] = useState(ad?.headline ?? '')
  const [body, setBody] = useState(ad?.body ?? '')
  const [kind, setKind] = useState<AdKind>(ad?.kind ?? 'Side')
  const [placement, setPlacement] = useState<AdPlacement>(ad?.placement ?? 'Right')
  const [percent, setPercent] = useState(ad?.spacePercent || 25)
  const [popupSeconds, setPopupSeconds] = useState(ad?.popupSeconds || 10)
  const [popupEvery, setPopupEvery] = useState(ad?.popupEveryMinutes || 5)
  const [background, setBackground] = useState(ad?.background ?? '#fff7e6')
  const [textColor, setTextColor] = useState(ad?.textColor ?? '#1f2937')
  const [picture, setPicture] = useState<{ id: string; url: string } | null>(ad?.mediaFileId && ad.mediaUrl ? { id: ad.mediaFileId, url: ad.mediaUrl } : null)
  const [picking, setPicking] = useState(false)
  const [start, setStart] = useState(utcToWallTime(ad?.startAt, zone))
  const [end, setEnd] = useState(utcToWallTime(ad?.endAt, zone))
  const [limited, setLimited] = useState(ad?.dailyStartMinutes != null)
  const [from, setFrom] = useState(ad?.dailyStartMinutes != null ? fromMinutes(ad.dailyStartMinutes) : '09:00')
  const [until, setUntil] = useState(ad?.dailyEndMinutes != null ? fromMinutes(ad.dailyEndMinutes) : '17:00')
  const [days, setDays] = useState(String(ad?.activeDays ?? ALL_DAYS))
  const [problem, setProblem] = useState<string | null>(null)

  const isEdge = kind === 'Side' || kind === 'Mega'
  const places = isEdge ? EDGES : CORNERS
  const range = kind === 'Mega' ? [50, 80] : [10, 40]

  const changeKind = (next: AdKind) => {
    setKind(next)
    if (next === 'Side' || next === 'Mega') {
      if (!EDGES.some((e) => e.value === placement)) setPlacement('Right')
      setPercent(next === 'Mega' ? 60 : 25)
    } else if (next === 'Minor' && !CORNERS.some((c) => c.value === placement)) {
      setPlacement('BottomRight')
    }
  }

  const submit = (event: FormEvent) => {
    event.preventDefault()
    const startIso = wallTimeToUtcIso(start, zone)
    const endIso = wallTimeToUtcIso(end, zone)
    if (!advertiser.trim()) return setProblem('Give the business name of the advertiser.')
    if (!headline.trim()) return setProblem('Write a headline.')
    if (!startIso || !endIso) return setProblem('Choose when the ad starts and when it ends.')
    if (new Date(endIso) <= new Date(startIso)) return setProblem('The end must be after the start.')

    let daily: AdInput['daily'] = null
    if (limited) {
      const a = toMinutes(from)
      const b = toMinutes(until)
      if (a === null || b === null) return setProblem('Choose the hours of the day.')
      if (a === b) return setProblem('The hours cannot start and end at the same time.')
      daily = { startMinutes: a, endMinutes: b, days: Number(days) }
    }

    setProblem(null)
    onSubmit({
      advertiserName: advertiser.trim(),
      headline: headline.trim(),
      body: body.trim() || null,
      mediaFileId: allowPicture ? picture?.id ?? null : null,
      background,
      textColor,
      kind,
      placement: kind === 'Popup' ? 'Center' : placement,
      spacePercent: isEdge ? percent : 0,
      popupSeconds: kind === 'Popup' ? popupSeconds : 0,
      popupEveryMinutes: kind === 'Popup' ? popupEvery : 0,
      startAt: startIso,
      endAt: endIso,
      daily,
    })
  }

  const preview = {
    id: 'preview', shopId: '', shopName: '', source: '', advertiserName: advertiser || 'Advertiser', headline: headline || 'Your headline',
    body: body || null, mediaFileId: picture?.id ?? null, mediaUrl: picture?.url ?? null, background, textColor, kind, placement,
    spacePercent: percent, popupSeconds, popupEveryMinutes: popupEvery, startAt: '', endAt: '', dailyStartMinutes: null, dailyEndMinutes: null,
    activeDays: ALL_DAYS, status: 'Draft', decidedByName: null, decidedAt: null, decisionNote: null, createdByUserId: '', pricePerHour: null, shopSharePercent: null, stoppedAt: null, can: [],
  } as ShopAd

  return (
    <form onSubmit={submit} noValidate className="grid gap-6 rounded-xl border border-gray-200 bg-white p-5 lg:grid-cols-[minmax(0,1fr)_16rem]">
      <div className="space-y-4">
        {(problem || error) && (
          <div role="alert" className="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-800">
            {problem ?? error}
          </div>
        )}

        <Input id="ad-advertiser" label="Advertiser (business name)" value={advertiser} maxLength={80} onChange={(e) => setAdvertiser(e.target.value)} />
        <Input id="ad-headline" label="Headline" value={headline} maxLength={80} onChange={(e) => setHeadline(e.target.value)} />
        <div>
          <label htmlFor="ad-body" className="text-sm font-medium text-gray-700">
            Text (optional)
          </label>
          <textarea
            id="ad-body"
            rows={2}
            maxLength={300}
            value={body}
            onChange={(e) => setBody(e.target.value)}
            className="mt-1 w-full rounded-lg border-2 border-gray-300 px-3 py-2 text-gray-900 focus-visible:outline-none focus:border-blue-500"
          />
          <p className="text-xs text-gray-500">Do not put personal or health details in an ad.</p>
        </div>

        <div className="grid gap-4 sm:grid-cols-2">
          <Select id="ad-kind" label="Kind of ad" options={KINDS.map((k) => ({ value: k.value, label: k.label }))} value={kind} onChange={(e) => changeKind(e.target.value as AdKind)} helperText={KINDS.find((k) => k.value === kind)?.help} />
          {kind !== 'Popup' && (
            <Select id="ad-placement" label={isEdge ? 'Edge' : 'Corner'} options={places} value={placement} onChange={(e) => setPlacement(e.target.value as AdPlacement)} />
          )}
        </div>

        {isEdge && (
          <div>
            <label htmlFor="ad-percent" className="text-sm font-medium text-gray-700">
              Share of the screen: {percent}%
            </label>
            <input id="ad-percent" type="range" min={range[0]} max={range[1]} step={5} value={percent} onChange={(e) => setPercent(Number(e.target.value))} className="w-full" />
          </div>
        )}
        {kind === 'Popup' && (
          <div className="grid gap-4 sm:grid-cols-2">
            <Input id="ad-popup-seconds" type="number" min={3} max={30} label="Stays for (seconds)" value={popupSeconds} onChange={(e) => setPopupSeconds(Number(e.target.value))} />
            <Input id="ad-popup-every" type="number" min={1} max={60} label="Comes back every (minutes)" value={popupEvery} onChange={(e) => setPopupEvery(Number(e.target.value))} />
          </div>
        )}

        <div className="grid gap-4 sm:grid-cols-2">
          <Input id="ad-background" type="color" label="Background" value={background} onChange={(e) => setBackground(e.target.value)} />
          <Input id="ad-text-colour" type="color" label="Text colour" value={textColor} onChange={(e) => setTextColor(e.target.value)} />
        </div>

        {allowPicture && (
          <div className="space-y-2">
            <p className="text-sm font-medium text-gray-700">Picture (optional)</p>
            {picture && (
              <div className="flex items-center gap-3">
                {/* eslint-disable-next-line @next/next/no-img-element */}
                <img src={picture.url} alt="Ad picture" className="h-12 max-w-[8rem] rounded border border-gray-200 object-contain" />
                <Button type="button" size="sm" variant="ghost" onClick={() => setPicture(null)}>
                  Remove picture
                </Button>
              </div>
            )}
            <Button type="button" size="sm" variant="secondary" onClick={() => setPicking(true)}>
              {picture ? 'Choose another picture' : 'Choose a picture from my files'}
            </Button>
          </div>
        )}

        <fieldset className="space-y-3 rounded-lg border border-gray-200 p-3">
          <legend className="px-1 text-sm font-medium text-gray-700">When</legend>
          <p className="text-xs text-gray-500">Times are on the shop clock: {zoneLabel(zone)}.</p>
          <div className="grid gap-4 sm:grid-cols-2">
            <Input id="ad-start" type="datetime-local" label="Starts" value={start} onChange={(e) => setStart(e.target.value)} />
            <Input id="ad-end" type="datetime-local" label="Ends" value={end} onChange={(e) => setEnd(e.target.value)} />
          </div>
          <label className="flex items-center gap-2 text-sm text-gray-800">
            <input type="checkbox" checked={limited} onChange={(e) => setLimited(e.target.checked)} />
            Only at certain hours each day
          </label>
          {limited && (
            <div className="grid gap-4 sm:grid-cols-3">
              <Input id="ad-from" type="time" label="From" value={from} onChange={(e) => setFrom(e.target.value)} />
              <Input id="ad-until" type="time" label="Until" value={until} onChange={(e) => setUntil(e.target.value)} />
              <Select id="ad-days" label="On" options={DAY_CHOICES} placeholder="Every day" value={days} onChange={(e) => setDays(e.target.value)} />
            </div>
          )}
        </fieldset>

        <div className="flex gap-3">
          <Button type="submit" isLoading={busy}>
            {ad ? 'Save changes' : 'Save as draft'}
          </Button>
          <Button type="button" variant="secondary" onClick={onCancel} disabled={busy}>
            Cancel
          </Button>
        </div>
      </div>

      <aside aria-label="Preview">
        <p className="mb-2 text-sm font-medium text-gray-700">Preview</p>
        <div className="aspect-square w-full overflow-hidden rounded-lg border border-gray-200" data-testid="ad-preview">
          <AdTile ad={preview} testId="ad-preview-tile" />
        </div>
      </aside>

      {picking && (
        <MediaPickerDialog
          kind="image"
          onSelect={(item: MediaLibraryItem) => {
            setPicking(false)
            setPicture({ id: item.id, url: item.url })
          }}
          onClose={() => setPicking(false)}
        />
      )}
    </form>
  )
}

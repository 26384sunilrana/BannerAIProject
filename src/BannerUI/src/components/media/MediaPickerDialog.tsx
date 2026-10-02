'use client'

import React, { useEffect, useState } from 'react'
import { Button, Input } from '@/components/Common'
import { MediaThumb } from '@/components/media/MediaThumb'
import { useDebounce } from '@/hooks/useDebounce'
import { mediaService, MediaLibraryItem } from '@/api/mediaService'
import { getErrorMessage } from '@/api/client'

interface Props {
  kind: 'image' | 'video'
  onSelect: (item: MediaLibraryItem) => void
  onClose: () => void
}

/** The shop's own files, to pick one for an image or video component instead of uploading it again. */
export function MediaPickerDialog({ kind, onSelect, onClose }: Props) {
  const [search, setSearch] = useState('')
  const term = useDebounce(search, 300)
  const [items, setItems] = useState<MediaLibraryItem[] | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    let cancelled = false
    mediaService
      .list({ type: kind, search: term, pageSize: 24 })
      .then((page) => {
        if (!cancelled) {
          setItems(page.items)
          setError(null)
        }
      })
      .catch((err) => {
        if (!cancelled) setError(getErrorMessage(err, 'Could not load your files.'))
      })
    return () => {
      cancelled = true
    }
  }, [kind, term])

  return (
    <div role="dialog" aria-modal="true" aria-label={`Choose ${kind === 'image' ? 'an image' : 'a video'}`} className="fixed inset-0 z-50 flex items-center justify-center bg-black bg-opacity-50 p-4">
      <div className="flex max-h-[90vh] w-full max-w-3xl flex-col rounded-lg bg-white shadow-xl">
        <header className="flex items-center justify-between gap-3 border-b border-gray-200 p-4">
          <h2 className="text-lg font-semibold text-gray-900">Choose {kind === 'image' ? 'an image' : 'a video'}</h2>
          <Button variant="ghost" size="sm" onClick={onClose}>
            Close
          </Button>
        </header>

        <div className="p-4">
          <Input id="picker-search" label="Search by name" value={search} onChange={(e) => setSearch(e.target.value)} />
        </div>

        <div className="overflow-y-auto px-4 pb-4">
          {error && (
            <p role="alert" className="text-red-700">
              {error}
            </p>
          )}
          {!items && !error && <p className="text-gray-500">Loading…</p>}
          {items?.length === 0 && (
            <p className="text-gray-500">{term ? 'Nothing matches that name.' : `You have not uploaded any ${kind === 'image' ? 'images' : 'videos'} yet.`}</p>
          )}
          <ul className="grid grid-cols-2 gap-3 sm:grid-cols-3">
            {items?.map((item) => (
              <li key={item.id}>
                <button
                  type="button"
                  data-testid={`pick-${item.fileName}`}
                  onClick={() => onSelect(item)}
                  className="w-full rounded-lg border border-gray-200 p-2 text-left hover:border-blue-500 focus:outline-none focus-visible:ring-2 focus-visible:ring-blue-500"
                >
                  <MediaThumb item={item} />
                </button>
              </li>
            ))}
          </ul>
        </div>
      </div>
    </div>
  )
}

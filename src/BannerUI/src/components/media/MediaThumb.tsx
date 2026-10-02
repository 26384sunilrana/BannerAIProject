'use client'

import React from 'react'
import { MediaLibraryItem } from '@/api/mediaService'
import { formatBytes, formatDuration } from '@/lib/format'

/** The picture (or the first frame of the video) with a line saying what the file is. */
export function MediaThumb({ item }: { item: MediaLibraryItem }) {
  const isVideo = item.fileType === 2
  const details = [
    formatBytes(item.sizeBytes),
    item.width && item.height ? `${item.width}×${item.height}` : null,
    isVideo && item.durationSeconds ? formatDuration(item.durationSeconds) : null,
  ].filter(Boolean)

  return (
    <>
      <div className="aspect-video w-full overflow-hidden rounded-lg bg-gray-100">
        {isVideo ? (
          <video src={item.url} preload="metadata" muted className="h-full w-full object-contain" aria-label={item.fileName} />
        ) : (
          // eslint-disable-next-line @next/next/no-img-element
          <img src={item.url} alt={item.fileName} loading="lazy" className="h-full w-full object-contain" />
        )}
      </div>
      <p className="mt-2 truncate text-sm font-medium text-gray-900" title={item.fileName}>
        {item.fileName}
      </p>
      <p className="text-xs text-gray-500">{details.join(' · ')}</p>
    </>
  )
}

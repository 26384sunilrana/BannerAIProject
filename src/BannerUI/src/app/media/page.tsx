'use client'

import React, { useCallback, useEffect, useRef, useState } from 'react'
import { AppShell } from '@/components/layout/AppShell'
import { Button, ConfirmDialog, Input, Select, Toast } from '@/components/Common'
import { Pagination } from '@/components/admin/Pagination'
import { MediaThumb } from '@/components/media/MediaThumb'
import { useDebounce } from '@/hooks/useDebounce'
import { useToast } from '@/hooks/useToast'
import { useMediaUpload } from '@/hooks/useMediaUpload'
import { mediaService, type MediaLibraryPage as LibraryPage, type MediaUsage } from '@/api/mediaService'
import { getErrorMessage } from '@/api/client'
import { formatBytes } from '@/lib/format'
import { Roles } from '@/lib/session'

const PAGE_SIZE = 24

const TYPE_OPTIONS = [
  { value: 'image', label: 'Images' },
  { value: 'video', label: 'Videos' },
]

export default function MediaLibraryPage() {
  return (
    <AppShell roles={[Roles.ShopOwner, Roles.SalesExecutive]}>
      <Library />
    </AppShell>
  )
}

function Library() {
  const toast = useToast()
  const upload = useMediaUpload()
  const fileInput = useRef<HTMLInputElement>(null)

  const [type, setType] = useState('')
  const [search, setSearch] = useState('')
  const [page, setPage] = useState(1)
  const [result, setResult] = useState<LibraryPage | null>(null)
  const [usage, setUsage] = useState<MediaUsage | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [toDelete, setToDelete] = useState<{ id: string; name: string } | null>(null)
  const [deleting, setDeleting] = useState(false)

  const term = useDebounce(search, 300)

  const load = useCallback(async () => {
    try {
      const [files, used] = await Promise.all([
        mediaService.list({ type: (type || undefined) as 'image' | 'video' | undefined, search: term, page, pageSize: PAGE_SIZE }),
        mediaService.usage(),
      ])
      setResult(files)
      setUsage(used)
      setError(null)
    } catch (err) {
      setError(getErrorMessage(err, 'Could not load your files.'))
    }
  }, [type, term, page])

  useEffect(() => {
    load()
  }, [load])

  useEffect(() => {
    setPage(1)
  }, [type, term])

  const onChoose = async (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0]
    event.target.value = ''
    if (!file) return

    const done = await upload.upload(file, file.type.startsWith('video/') ? 'video' : 'image')
    if (done) {
      toast.success(`${file.name} uploaded`)
      await load()
    }
  }

  const confirmDelete = async () => {
    if (!toDelete) return
    setDeleting(true)
    try {
      await mediaService.remove(toDelete.id)
      toast.success(`${toDelete.name} deleted`)
      setToDelete(null)
      await load()
    } catch (err) {
      setToDelete(null)
      toast.error(getErrorMessage(err, 'The file could not be deleted.'))
    } finally {
      setDeleting(false)
    }
  }

  const percent = usage?.limitBytes ? Math.min(100, Math.round((usage.usedBytes / usage.limitBytes) * 100)) : null
  const uploading = upload.isUploading

  return (
    <div className="space-y-6">
      <Toast messages={toast.messages} onRemove={toast.remove} />

      <header className="flex flex-wrap items-end justify-between gap-3">
        <div>
          <h1 className="text-2xl font-semibold text-gray-900">My files</h1>
          <p className="mt-1 text-gray-600">Pictures and videos you have uploaded. Use them in any banner without uploading again.</p>
        </div>
        <div>
          <input ref={fileInput} type="file" accept="image/png,image/jpeg,image/gif,image/webp,video/mp4,video/webm" className="sr-only" aria-label="Upload a file" onChange={onChoose} />
          <Button onClick={() => fileInput.current?.click()} isLoading={uploading}>
            Upload a file
          </Button>
        </div>
      </header>

      {uploading && (
        <div role="progressbar" aria-valuenow={upload.progress} aria-valuemin={0} aria-valuemax={100} className="h-2 w-full rounded bg-gray-200">
          <div className="h-2 rounded bg-blue-600" style={{ width: `${upload.progress}%` }} />
        </div>
      )}
      {upload.error && (
        <p role="alert" className="text-red-700">
          {upload.error}
        </p>
      )}

      {usage && (
        <section aria-label="Storage" className="rounded-xl border border-gray-200 bg-white p-4" data-testid="storage-usage">
          <p className="text-sm text-gray-700">
            <span className="font-semibold text-gray-900">{formatBytes(usage.usedBytes)}</span>
            {usage.limitBytes ? ` of ${formatBytes(usage.limitBytes)} used` : ' used'} · {usage.imageCount} image{usage.imageCount === 1 ? '' : 's'} ·{' '}
            {usage.videoCount} video{usage.videoCount === 1 ? '' : 's'}
          </p>
          {percent !== null && (
            <div role="progressbar" aria-label="Storage used" aria-valuenow={percent} aria-valuemin={0} aria-valuemax={100} className="mt-2 h-2 w-full rounded bg-gray-200">
              <div className={`h-2 rounded ${percent >= 90 ? 'bg-red-500' : percent >= 70 ? 'bg-amber-500' : 'bg-blue-600'}`} style={{ width: `${percent}%` }} />
            </div>
          )}
        </section>
      )}

      <div className="flex flex-wrap items-end gap-4">
        <div className="w-56">
          <Select id="media-type" label="Show" options={TYPE_OPTIONS} placeholder="Images and videos" value={type} onChange={(e) => setType(e.target.value)} />
        </div>
        <div className="w-full max-w-sm">
          <Input id="media-search" label="Search by name" value={search} onChange={(e) => setSearch(e.target.value)} />
        </div>
      </div>

      {error && (
        <p role="alert" className="text-red-700">
          {error}
        </p>
      )}
      {!result && !error && <p className="text-gray-500">Loading your files…</p>}
      {result?.items.length === 0 && (
        <p className="rounded-xl border border-dashed border-gray-300 p-8 text-center text-gray-500">
          {term || type ? 'No file matches.' : 'You have not uploaded anything yet. Use “Upload a file” above.'}
        </p>
      )}

      {result && result.items.length > 0 && (
        <ul className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4">
          {result.items.map((item) => (
            <li key={item.id} data-testid={`file-${item.fileName}`} className="rounded-xl border border-gray-200 bg-white p-3">
              <MediaThumb item={item} />
              <div className="mt-2 flex items-center justify-between gap-2">
                {item.inUse ? (
                  <span className="rounded-full bg-green-100 px-2 py-0.5 text-xs font-medium text-green-800">Used in a banner</span>
                ) : (
                  <span className="text-xs text-gray-500">Not used</span>
                )}
                <Button size="sm" variant="ghost" onClick={() => setToDelete({ id: item.id, name: item.fileName })}>
                  Delete
                </Button>
              </div>
            </li>
          ))}
        </ul>
      )}

      {result && <Pagination page={result.page} pageSize={result.pageSize} total={result.total} onChange={setPage} />}

      <ConfirmDialog
        isOpen={toDelete !== null}
        title="Delete this file?"
        message={`${toDelete?.name ?? ''} will be removed for good. If a banner still uses it, it cannot be deleted and you will be told which banners.`}
        confirmText="Delete"
        isDangerous
        isLoading={deleting}
        onCancel={() => setToDelete(null)}
        onConfirm={confirmDelete}
      />
    </div>
  )
}

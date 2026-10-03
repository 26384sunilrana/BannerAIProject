'use client'

import React, { useState } from 'react'
import { Button, ConfirmDialog, Input } from '@/components/Common'
import { TimeZoneSelect } from '@/components/location/TimeZoneSelect'
import { LocationItem, NewLocation } from '@/types/location'

export type AddKind = 'country' | 'state' | 'name'

interface Props {
  title: string
  /** What one item is called, for the buttons and messages. */
  noun: string
  addKind: AddKind
  items: LocationItem[] | null
  /** Why nothing can be added or listed yet, for example "Choose a state first". Shown instead of the list. */
  blockedReason?: string
  selectedId: string | null
  /** What sits directly under an item, for the count line. */
  childNoun?: string
  busy: boolean
  onSelect: (item: LocationItem) => void
  onAdd: (value: NewLocation) => Promise<boolean>
  onRename: (item: LocationItem, name: string) => Promise<boolean>
  onToggle: (item: LocationItem) => Promise<void>
  onDelete: (item: LocationItem) => Promise<void>
  /** Countries and cities can carry a time zone; give this to show and change it. */
  onSetTimeZone?: (item: LocationItem, timeZoneId: string | null) => Promise<boolean>
}

export function LocationColumn({
  title, noun, addKind, items, blockedReason, selectedId, childNoun, busy, onSelect, onAdd, onRename, onToggle, onDelete, onSetTimeZone,
}: Props) {
  const [name, setName] = useState('')
  const [code, setCode] = useState('')
  const [editing, setEditing] = useState<{ id: string; name: string } | null>(null)
  const [toDelete, setToDelete] = useState<LocationItem | null>(null)
  const [zoneFor, setZoneFor] = useState<{ id: string; value: string } | null>(null)

  const needsCode = addKind !== 'name'
  const canAdd = !blockedReason && name.trim().length > 0 && (!needsCode || code.trim().length > 0)

  const add = async (event: React.FormEvent) => {
    event.preventDefault()
    if (!canAdd) return
    if (await onAdd({ name: name.trim(), code: needsCode ? code.trim() : undefined })) {
      setName('')
      setCode('')
    }
  }

  const saveRename = async () => {
    if (!editing) return
    const item = items?.find((i) => i.id === editing.id)
    if (item && (await onRename(item, editing.name.trim()))) setEditing(null)
  }

  return (
    <section aria-label={title} className="flex min-w-0 flex-col rounded-xl border border-gray-200 bg-white">
      <header className="border-b border-gray-100 px-4 py-3">
        <h2 className="font-semibold text-gray-900">{title}</h2>
      </header>

      {blockedReason ? (
        <p className="px-4 py-6 text-sm text-gray-500">{blockedReason}</p>
      ) : (
        <>
          <form onSubmit={add} className="space-y-2 border-b border-gray-100 p-3">
            <div className="flex gap-2">
              {needsCode && (
                <div className="w-24 shrink-0">
                  <Input
                    aria-label={`${noun} code`}
                    placeholder="Code"
                    maxLength={addKind === 'country' ? 2 : 10}
                    value={code}
                    onChange={(e) => setCode(e.target.value)}
                  />
                </div>
              )}
              <Input aria-label={`New ${noun} name`} placeholder={`New ${noun}`} value={name} onChange={(e) => setName(e.target.value)} />
            </div>
            <Button type="submit" size="sm" disabled={!canAdd || busy}>
              Add {noun}
            </Button>
          </form>

          {items === null && <p className="px-4 py-6 text-sm text-gray-500">Loading…</p>}
          {items?.length === 0 && <p className="px-4 py-6 text-sm text-gray-500">Nothing here yet.</p>}

          <ul className="max-h-[28rem] divide-y divide-gray-100 overflow-y-auto">
            {items?.map((item) => (
              <li
                key={item.id}
                data-testid={`${noun.toLowerCase()}-${item.name}`}
                className={`px-3 py-2 ${selectedId === item.id ? 'bg-blue-50' : ''}`}
              >
                {editing?.id === item.id ? (
                  <div className="flex gap-2">
                    <Input
                      aria-label={`Rename ${item.name}`}
                      value={editing.name}
                      onChange={(e) => setEditing({ id: item.id, name: e.target.value })}
                    />
                    <Button size="sm" onClick={saveRename} disabled={busy || editing.name.trim().length < 2}>
                      Save
                    </Button>
                    <Button size="sm" variant="ghost" onClick={() => setEditing(null)}>
                      Cancel
                    </Button>
                  </div>
                ) : (
                  <>
                    <button type="button" className="block w-full text-left" onClick={() => onSelect(item)}>
                      <span className={`font-medium ${item.isActive ? 'text-gray-900' : 'text-gray-400 line-through'}`}>{item.name}</span>
                      {!item.isActive && <span className="ml-2 rounded-full bg-gray-100 px-2 py-0.5 text-xs text-gray-600">Off</span>}
                      <span className="block font-mono text-xs text-gray-500">{item.uniqueId ?? 'no identifier yet'}</span>
                      {onSetTimeZone && (
                        <span className="block text-xs text-gray-500" data-testid="item-time-zone">
                          {item.timeZoneId ? `Time zone: ${item.timeZoneId}` : 'No time zone set'}
                        </span>
                      )}
                      <span className="block text-xs text-gray-500">
                        {childNoun ? `${item.childCount} ${childNoun}${item.childCount === 1 ? '' : 's'} · ` : ''}
                        {item.shopCount} shop{item.shopCount === 1 ? '' : 's'}
                      </span>
                    </button>
                    <div className="mt-1 flex gap-1">
                      <Button size="sm" variant="ghost" onClick={() => setEditing({ id: item.id, name: item.name })}>
                        Rename
                      </Button>
                      {onSetTimeZone && (
                        <Button size="sm" variant="ghost" onClick={() => setZoneFor({ id: item.id, value: item.timeZoneId ?? '' })}>
                          Time zone
                        </Button>
                      )}
                      <Button size="sm" variant="ghost" disabled={busy} onClick={() => onToggle(item)}>
                        {item.isActive ? 'Switch off' : 'Switch on'}
                      </Button>
                      <Button size="sm" variant="ghost" disabled={busy} onClick={() => setToDelete(item)}>
                        Delete
                      </Button>
                    </div>
                  </>
                )}
                {zoneFor?.id === item.id && onSetTimeZone && (
                  <div className="mt-2 space-y-2 rounded-lg border border-gray-200 bg-gray-50 p-2">
                    <TimeZoneSelect
                      id={`zone-${item.id}`}
                      label={`Time zone of ${item.name}`}
                      value={zoneFor.value}
                      onChange={(value) => setZoneFor({ id: item.id, value })}
                      inheritLabel="Not set"
                      alwaysOpen
                    />
                    <div className="flex gap-2">
                      <Button
                        size="sm"
                        disabled={busy}
                        onClick={async () => {
                          if (await onSetTimeZone(item, zoneFor.value || null)) setZoneFor(null)
                        }}
                      >
                        Save time zone
                      </Button>
                      <Button size="sm" variant="ghost" onClick={() => setZoneFor(null)}>
                        Cancel
                      </Button>
                    </div>
                  </div>
                )}
              </li>
            ))}
          </ul>
        </>
      )}

      <ConfirmDialog
        isOpen={toDelete !== null}
        title={`Delete this ${noun.toLowerCase()}?`}
        message={`${toDelete?.name ?? ''} will be removed for good. This only works when nothing is under it; otherwise switch it off.`}
        confirmText="Delete"
        isDangerous
        onCancel={() => setToDelete(null)}
        onConfirm={async () => {
          const target = toDelete
          setToDelete(null)
          if (target) await onDelete(target)
        }}
      />
    </section>
  )
}

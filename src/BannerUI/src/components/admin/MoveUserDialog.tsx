'use client'

import React, { useEffect, useState } from 'react'
import { Button, Select } from '@/components/Common'
import { adminService } from '@/api/adminService'
import { getErrorMessage } from '@/api/client'

interface Props {
  /** Who is being moved, for the heading. */
  userLabel: string
  /** The shop the person is in now, left out of the choices. */
  currentShopId: string | null
  onMove: (shopId: string) => Promise<void>
  onClose: () => void
}

/** Chooses another shop for a sales executive. The API checks the shop has a subscription and a free login. */
export function MoveUserDialog({ userLabel, currentShopId, onMove, onClose }: Props) {
  const [shops, setShops] = useState<{ id: string; name: string }[] | null>(null)
  const [shopId, setShopId] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  useEffect(() => {
    adminService
      .listShopOptions()
      .then((list) => setShops(list.filter((s) => s.id.toLowerCase() !== currentShopId?.toLowerCase())))
      .catch((err) => setError(getErrorMessage(err, 'Could not load the shops.')))
  }, [currentShopId])

  const submit = async (event: React.FormEvent) => {
    event.preventDefault()
    if (!shopId) {
      setError('Choose the shop to move them to.')
      return
    }
    setBusy(true)
    setError(null)
    try {
      await onMove(shopId)
    } catch (err) {
      setError(getErrorMessage(err, 'They could not be moved.'))
      setBusy(false)
    }
  }

  return (
    <div role="dialog" aria-modal="true" aria-label="Move to another shop" className="fixed inset-0 z-50 flex items-center justify-center bg-black bg-opacity-50 p-4">
      <form onSubmit={submit} noValidate className="w-full max-w-md space-y-4 rounded-lg bg-white p-6 shadow-xl">
        <h2 className="text-xl font-bold text-gray-900">Move {userLabel}</h2>
        <p className="text-gray-600">
          They leave their current shop and become a sales executive of the one you choose. They are signed out and sign in again.
        </p>
        {error && (
          <div role="alert" className="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-800">
            {error}
          </div>
        )}
        <Select
          id="move-shop"
          label="Move to"
          options={(shops ?? []).map((s) => ({ value: s.id, label: s.name }))}
          value={shopId}
          onChange={(e) => setShopId(e.target.value)}
          disabled={!shops}
        />
        <div className="flex justify-end gap-3">
          <Button type="button" variant="secondary" onClick={onClose} disabled={busy}>
            Cancel
          </Button>
          <Button type="submit" isLoading={busy}>
            Move
          </Button>
        </div>
      </form>
    </div>
  )
}

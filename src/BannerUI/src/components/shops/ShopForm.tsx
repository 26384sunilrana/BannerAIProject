'use client'

import React, { useState } from 'react'
import { Button, Input } from '@/components/Common'
import { LocationPicker, PickedLocation } from '@/components/location/LocationPicker'
import { TimeZoneSelect } from '@/components/location/TimeZoneSelect'
import { forgetShopTimeZone } from '@/hooks/useShopTimeZone'
import { apiClient } from '@/api/client'
import { useShops } from '@/hooks/useShops'
import { locationService } from '@/api/locationService'
import { getErrorMessage } from '@/api/client'
import { ShopDto } from '@/types/shop'
import Link from 'next/link'

interface Props {
  /** The shop being edited; leave out to create a new one (administrators only). */
  shop?: ShopDto
  onSaved: (shopId: string) => void
  onCancel: () => void
}

export function ShopForm({ shop, onSaved, onCancel }: Props) {
  const { createShop, updateShop } = useShops()

  const [name, setName] = useState(shop?.name ?? '')
  const [description, setDescription] = useState(shop?.description ?? '')
  const [phoneNumber, setPhoneNumber] = useState(shop?.phoneNumber ?? '')
  const [website, setWebsite] = useState(shop?.website ?? '')
  const [address, setAddress] = useState(shop?.address ?? '')
  const [postalCode, setPostalCode] = useState(shop?.postalCode ?? '')
  const [place, setPlace] = useState<PickedLocation>({
    countryCode: shop?.countryCode ?? 'IN',
    stateId: shop?.stateId ?? undefined,
    cityId: shop?.cityId ?? undefined,
    groupId: shop?.groupId ?? undefined,
  })
  const [timeZone, setTimeZone] = useState(shop?.ownTimeZoneId ?? '')
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const submit = async (event: React.FormEvent) => {
    event.preventDefault()
    if (name.trim().length < 2) {
      setError('Give the shop a name.')
      return
    }

    setSaving(true)
    setError(null)
    try {
      const details = {
        name: name.trim(),
        description: description.trim() || undefined,
        phoneNumber: phoneNumber.trim() || undefined,
        website: website.trim() || undefined,
        address: address.trim() || undefined,
        postalCode: postalCode.trim() || undefined,
        city: shop?.city || undefined,
        countryCode: place.countryCode,
        stateId: place.stateId,
        districtId: shop?.districtId,
        latitude: shop?.latitude,
        longitude: shop?.longitude,
      }

      let shopId = shop?.id
      if (shop) {
        await updateShop(shop.id, { ...details, status: shop.status })
      } else {
        shopId = (await createShop(details)).id
      }

      // the city and group are saved on their own, so the group is checked against the city
      if (place.cityId && shopId) await locationService.setShopLocation(shopId, place.cityId, place.groupId ?? null)

      // the time zone is kept apart from the rest of the shop; empty goes back to following the city
      if (shopId && (timeZone || shop?.ownTimeZoneId)) {
        await apiClient.put(`/shops/${shopId}/time-zone`, { timeZoneId: timeZone || null })
        forgetShopTimeZone(shopId)
      }

      onSaved(shopId!)
    } catch (err) {
      setError(err instanceof Error && !(err as { response?: unknown }).response ? err.message : getErrorMessage(err, 'The shop could not be saved.'))
    } finally {
      setSaving(false)
    }
  }

  return (
    <form onSubmit={submit} className="max-w-2xl space-y-6" noValidate>
      {error && (
        <p role="alert" className="rounded-lg border border-red-200 bg-red-50 p-3 text-sm text-red-800">
          {error}
          {/takeover/i.test(error) && (
            <>
              {' '}
              <Link href="/takeover" className="font-medium underline">
                Go to Takeover
              </Link>
            </>
          )}
        </p>
      )}

      <div className="space-y-4">
        <Input id="shop-name" label="Shop name" value={name} onChange={(e) => setName(e.target.value)} />
        <Input id="shop-description" label="Description" value={description} onChange={(e) => setDescription(e.target.value)} />
        <div className="grid gap-4 sm:grid-cols-2">
          <Input id="shop-phone" label="Phone" value={phoneNumber} onChange={(e) => setPhoneNumber(e.target.value)} />
          <Input id="shop-website" label="Website" value={website} onChange={(e) => setWebsite(e.target.value)} />
        </div>
      </div>

      <fieldset className="space-y-4">
        <legend className="text-lg font-semibold text-gray-900">Where it is</legend>
        <LocationPicker value={place} onChange={setPlace} />
        <Input id="shop-address" label="Street address" value={address} onChange={(e) => setAddress(e.target.value)} />
        <div className="max-w-xs">
          <Input id="shop-postal" label="Postal code" value={postalCode} onChange={(e) => setPostalCode(e.target.value)} />
        </div>
        <div className="max-w-md">
          <TimeZoneSelect
            id="shop-time-zone"
            label="Time zone"
            value={timeZone}
            onChange={setTimeZone}
            inheritLabel={`Same as my city or country${shop?.timeZoneId && !shop.ownTimeZoneId ? ` (${shop.timeZoneId})` : ''}`}
            helperText="Banner times and the schedule calendar use your shop's clock."
          />
        </div>
      </fieldset>

      <div className="flex gap-3">
        <Button type="submit" isLoading={saving}>
          {shop ? 'Save changes' : 'Create shop'}
        </Button>
        <Button type="button" variant="secondary" onClick={onCancel} disabled={saving}>
          Cancel
        </Button>
      </div>
    </form>
  )
}

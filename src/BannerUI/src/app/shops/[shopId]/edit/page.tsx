'use client'

import React, { useEffect, useState } from 'react'
import { useParams, useRouter } from 'next/navigation'
import { ShopForm } from '@/components/shops/ShopForm'
import { useShops } from '@/hooks/useShops'
import { ShopDto } from '@/types/shop'

export default function EditShopPage() {
  const router = useRouter()
  const { shopId } = useParams() as { shopId: string }
  const { getShopById } = useShops()
  const [shop, setShop] = useState<ShopDto | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    getShopById(shopId)
      .then(setShop)
      .catch(() => setError('The shop could not be loaded.'))
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [shopId])

  if (error) return <p role="alert" className="text-red-700">{error}</p>
  if (!shop) return <p className="text-gray-500">Loading the shop…</p>

  return (
    <div className="space-y-6">
      <header>
        <h1 className="text-2xl font-semibold text-gray-900">Edit {shop.name}</h1>
        {shop.uniqueId && <p className="mt-1 font-mono text-sm text-gray-500">{shop.uniqueId}</p>}
      </header>
      <ShopForm shop={shop} onSaved={(id) => router.push(`/shops/${id}`)} onCancel={() => router.back()} />
    </div>
  )
}

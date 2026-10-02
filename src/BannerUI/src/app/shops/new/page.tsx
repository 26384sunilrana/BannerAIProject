'use client'

import React from 'react'
import { useRouter } from 'next/navigation'
import { RequireAuth } from '@/components/auth/RequireAuth'
import { ShopForm } from '@/components/shops/ShopForm'
import { Roles } from '@/lib/session'

export default function NewShopPage() {
  const router = useRouter()

  return (
    <RequireAuth roles={[Roles.Admin]}>
      <div className="space-y-6">
        <header>
          <h1 className="text-2xl font-semibold text-gray-900">New shop</h1>
          <p className="mt-1 text-gray-600">
            Shop owners create their own shop when they sign up. Use this to add one for them; they get an identifier when they take a subscription.
          </p>
        </header>
        <ShopForm onSaved={(id) => router.push(`/shops/${id}`)} onCancel={() => router.back()} />
      </div>
    </RequireAuth>
  )
}

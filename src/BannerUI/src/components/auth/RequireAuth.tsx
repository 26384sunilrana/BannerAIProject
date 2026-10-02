'use client'

import React, { useEffect } from 'react'
import { usePathname, useRouter } from 'next/navigation'
import { useAuth } from '@/context/AuthContext'

interface RequireAuthProps {
  children: React.ReactNode
  /** Any one of these roles is enough. Omit to allow every signed-in user. */
  roles?: string[]
}

/** Sends visitors without a session to the sign-in page and blocks pages their role does not allow. */
export function RequireAuth({ children, roles }: RequireAuthProps) {
  const { user, ready, hasRole, signedOutRedirect } = useAuth()
  const router = useRouter()
  const pathname = usePathname()

  useEffect(() => {
    if (ready && !user) {
      router.replace(signedOutRedirect ?? `/login?returnUrl=${encodeURIComponent(pathname || '/')}`)
    }
  }, [ready, user, router, pathname, signedOutRedirect])

  if (!ready || !user) {
    return <p className="p-8 text-gray-500">Loading…</p>
  }

  if (roles && !roles.some((role) => hasRole(role))) {
    return (
      <div className="max-w-xl mx-auto p-8">
        <h1 className="text-xl font-semibold text-gray-900">Not available</h1>
        <p className="mt-2 text-gray-600">Your account does not have access to this page.</p>
      </div>
    )
  }

  return <>{children}</>
}

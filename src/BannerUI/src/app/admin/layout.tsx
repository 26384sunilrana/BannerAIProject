'use client'

import React from 'react'
import { AppShell } from '@/components/layout/AppShell'
import { Roles } from '@/lib/session'

/** Everything under /admin is for platform admins only. */
export default function AdminLayout({ children }: { children: React.ReactNode }) {
  return <AppShell roles={[Roles.Admin]}>{children}</AppShell>
}

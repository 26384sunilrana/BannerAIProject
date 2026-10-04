import { Suspense } from 'react'
import { AdminsPanel } from './AdminsPanel'

export default function AdminsPage() {
  return (
    <Suspense fallback={null}>
      <AdminsPanel />
    </Suspense>
  )
}

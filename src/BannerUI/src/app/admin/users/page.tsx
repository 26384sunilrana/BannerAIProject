import { Suspense } from 'react'
import { UsersPanel } from './UsersPanel'

export default function AdminUsersPage() {
  return (
    <Suspense fallback={null}>
      <UsersPanel />
    </Suspense>
  )
}

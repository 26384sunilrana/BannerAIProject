import { Suspense } from 'react'
import { AuditPanel } from './AuditPanel'

export default function AuditLogPage() {
  return (
    <Suspense fallback={null}>
      <AuditPanel />
    </Suspense>
  )
}

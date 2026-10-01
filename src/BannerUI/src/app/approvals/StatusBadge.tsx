import React from 'react'
import { WorkflowStatus } from '@/types/workflow'

const STYLES: Record<WorkflowStatus, { label: string; className: string }> = {
  Draft: { label: 'Draft', className: 'bg-gray-100 text-gray-800' },
  PendingApproval: { label: 'Waiting for approval', className: 'bg-yellow-100 text-yellow-800' },
  Approved: { label: 'Approved', className: 'bg-blue-100 text-blue-800' },
  Published: { label: 'Live', className: 'bg-green-100 text-green-800' },
  Rejected: { label: 'Rejected', className: 'bg-red-100 text-red-800' },
  Unpublished: { label: 'Taken down', className: 'bg-gray-100 text-gray-800' },
  Archived: { label: 'Archived', className: 'bg-gray-100 text-gray-600' },
}

export function StatusBadge({ status }: { status: WorkflowStatus }) {
  const style = STYLES[status] ?? { label: status, className: 'bg-gray-100 text-gray-800' }
  return <span className={`rounded-full px-2.5 py-0.5 text-xs font-medium ${style.className}`}>{style.label}</span>
}

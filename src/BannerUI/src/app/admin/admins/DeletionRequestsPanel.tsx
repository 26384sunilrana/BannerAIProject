'use client'

import React, { useState } from 'react'
import { adminService } from '@/api/adminService'
import { getErrorMessage } from '@/api/client'
import { AdminDeletionRequest } from '@/types/admin'
import { Button, ConfirmDialog } from '@/components/Common'

export function DeletionRequestsPanel({
  requests,
  onUpdate,
  toast,
}: {
  requests: AdminDeletionRequest[]
  onUpdate: () => Promise<void>
  toast: { success: (msg: string) => void; error: (msg: string) => void }
}) {
  const [busyId, setBusyId] = useState<string | null>(null)
  const [toApprove, setToApprove] = useState<AdminDeletionRequest | null>(null)
  const [toReject, setToReject] = useState<AdminDeletionRequest | null>(null)

  const handleAction = async (work: () => Promise<unknown>, done: string) => {
    try {
      await work()
      toast.success(done)
      await onUpdate()
    } catch (err) {
      toast.error(getErrorMessage(err, 'That did not work.'))
    } finally {
      setBusyId(null)
    }
  }

  if (requests.length === 0) {
    return null
  }

  return (
    <div className="rounded-xl border border-amber-200 bg-amber-50 p-4">
      <h2 className="text-lg font-semibold text-amber-900 mb-4">Pending Deletion Requests</h2>

      <div className="space-y-3">
        {requests.map((request) => {
          const busy = busyId === request.id
          return (
            <div key={request.id} className="rounded-lg bg-white p-4 border border-amber-200">
              <div className="flex items-start justify-between gap-4">
                <div className="flex-1">
                  <p className="font-medium text-gray-900">{request.adminToDeleteName}</p>
                  <p className="text-sm text-gray-600">{request.adminToDeleteEmail}</p>
                  <p className="text-xs text-gray-500 mt-1">
                    Requested by{' '}
                    <span className="font-medium">{request.requestedByAdminEmail}</span> on{' '}
                    {new Date(request.createdAt).toLocaleDateString()}
                  </p>
                  {request.reason && (
                    <p className="text-sm text-gray-700 mt-2 italic">
                      Reason: <span className="font-medium">{request.reason}</span>
                    </p>
                  )}
                </div>

                <div className="flex gap-2">
                  <Button
                    size="sm"
                    variant="secondary"
                    isLoading={busy}
                    onClick={() => setToReject(request)}
                    title="Reject deletion request"
                  >
                    Reject
                  </Button>
                  <Button
                    size="sm"
                    variant="danger"
                    isLoading={busy}
                    onClick={() => setToApprove(request)}
                    title="Approve deletion request"
                  >
                    Approve
                  </Button>
                </div>
              </div>
            </div>
          )
        })}
      </div>

      {/* Approve Confirmation */}
      {toApprove && (
        <ConfirmDialog
          isOpen={toApprove !== null}
          title="Approve admin deletion?"
          message={`${toApprove.adminToDeleteEmail} will be deleted after you approve this request. This action is permanent.`}
          confirmText="Approve deletion"
          isDangerous
          onCancel={() => setToApprove(null)}
          onConfirm={async () => {
            const req = toApprove
            setToApprove(null)
            setBusyId(req.id)
            await handleAction(
              () => adminService.approveDeletion(req.id),
              `Deletion approved for ${req.adminToDeleteEmail}`
            )
          }}
        />
      )}

      {/* Reject Confirmation */}
      {toReject && (
        <ConfirmDialog
          isOpen={toReject !== null}
          title="Reject deletion request?"
          message={`${toReject.adminToDeleteEmail} will remain as an administrator.`}
          confirmText="Reject"
          onCancel={() => setToReject(null)}
          onConfirm={async () => {
            const req = toReject
            setToReject(null)
            setBusyId(req.id)
            await handleAction(
              () => adminService.rejectDeletion(req.id),
              `Deletion request rejected for ${req.adminToDeleteEmail}`
            )
          }}
        />
      )}
    </div>
  )
}

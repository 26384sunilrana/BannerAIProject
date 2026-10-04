'use client'

import React, { useCallback, useEffect, useState } from 'react'
import { Button, ConfirmDialog, Toast } from '@/components/Common'
import { useToast } from '@/hooks/useToast'
import { useAuth } from '@/context/AuthContext'
import { adminService } from '@/api/adminService'
import { getErrorMessage } from '@/api/client'
import { AdminUser, AdminDeletionRequest } from '@/types/admin'
import { CreateAdminDialog } from './CreateAdminDialog'
import { DeletionRequestsPanel } from './DeletionRequestsPanel'

export function AdminsPanel() {
  const { user: me } = useAuth()
  const toast = useToast()

  const [admins, setAdmins] = useState<AdminUser[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [busyId, setBusyId] = useState<string | null>(null)
  const [showCreateDialog, setShowCreateDialog] = useState(false)
  const [toDelete, setToDelete] = useState<AdminUser | null>(null)
  const [toRemove, setToRemove] = useState<AdminUser | null>(null)
  const [pendingRequests, setPendingRequests] = useState<AdminDeletionRequest[]>([])

  const loadAdmins = useCallback(async () => {
    try {
      setLoading(true)
      setError(null)
      const data = await adminService.listAllAdmins()
      setAdmins(data)

      // Load deletion requests if Super Admin
      if (me?.roles?.includes('SuperAdmin')) {
        try {
          const requests = await adminService.getPendingDeletionRequests()
          setPendingRequests(requests)
        } catch (err) {
          console.warn('Could not load deletion requests:', err)
        }
      }
    } catch (err) {
      const errorMsg = getErrorMessage(err, 'Could not load admins.')
      console.error('Error loading admins:', errorMsg, err)
      setError(errorMsg)
    } finally {
      setLoading(false)
    }
  }, [me?.id])

  useEffect(() => {
    loadAdmins()
  }, [loadAdmins])

  const handleRequestDeletion = async (admin: AdminUser) => {
    setToDelete(admin)
  }

  const handleConfirmDeletion = async (reason?: string) => {
    if (!toDelete) return
    setBusyId(toDelete.id)
    try {
      await adminService.requestAdminDeletion(toDelete.id, reason)
      toast.success(`Deletion request submitted for ${toDelete.email}`)
      setToDelete(null)
      await loadAdmins()
    } catch (err) {
      toast.error(getErrorMessage(err, 'Could not request deletion.'))
      setBusyId(null)
    }
  }

  const isSuperAdminAccount = me?.roles?.includes('SuperAdmin')

  return (
    <div className="space-y-6">
      <Toast messages={toast.messages} onRemove={toast.remove} />

      <header>
        <h1 className="text-2xl font-semibold text-gray-900">Administrators</h1>
        <p className="mt-1 text-gray-600">Manage global administrators and deletion requests.</p>
      </header>

      {error && (
        <p role="alert" className="text-red-700">
          {error}
        </p>
      )}

      {loading && <p className="text-gray-500">Loading administrators…</p>}

      {!loading && (
        <>
          {/* Create Admin Button */}
          <div className="flex justify-end">
            <Button variant="primary" onClick={() => setShowCreateDialog(true)}>
              + Create Administrator
            </Button>
          </div>

          {/* Admins Table */}
          <div className="overflow-x-auto rounded-xl border border-gray-200 bg-white">
            <table className="min-w-full divide-y divide-gray-200 text-sm">
              <thead className="bg-gray-50 text-left text-gray-600">
                <tr>
                  <th className="px-4 py-3 font-medium">Administrator</th>
                  <th className="px-4 py-3 font-medium">Role</th>
                  <th className="px-4 py-3 font-medium">Status</th>
                  <th className="px-4 py-3 font-medium">Created</th>
                  <th className="px-4 py-3 font-medium text-right">Actions</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-100">
                {admins.length === 0 && (
                  <tr>
                    <td colSpan={5} className="px-4 py-8 text-center text-gray-500">
                      No administrators found.
                    </td>
                  </tr>
                )}
                {admins.map((admin) => {
                  const isMe = admin.id === me?.id
                  const isSuperAdmin = admin.roles.includes('SuperAdmin')
                  const busy = busyId === admin.id
                  const hasDeletionRequest = pendingRequests.some((r) => r.adminIdToDelete === admin.id)

                  return (
                    <tr key={admin.id}>
                      <td className="px-4 py-3">
                        <p className="font-medium text-gray-900">{admin.fullName || admin.email}</p>
                        <p className="text-gray-600">{admin.email}</p>
                      </td>
                      <td className="px-4 py-3">
                        <span className="inline-flex rounded-full bg-blue-100 px-3 py-1 text-xs font-medium text-blue-900">
                          {isSuperAdmin ? 'Super Admin' : 'Global Admin'}
                        </span>
                        {isMe && <span className="ml-2 text-xs text-gray-500">(You)</span>}
                      </td>
                      <td className="px-4 py-3">
                        <div className="flex flex-wrap gap-1">
                          <StatusPill status={admin.isActive ? 'active' : 'inactive'}>
                            {admin.isActive ? 'Active' : 'Deactivated'}
                          </StatusPill>
                          {admin.isLockedOut && <StatusPill status="locked">Locked out</StatusPill>}
                          {hasDeletionRequest && <StatusPill status="warning">Deletion pending</StatusPill>}
                        </div>
                      </td>
                      <td className="px-4 py-3 text-gray-700">{new Date(admin.createdAt).toLocaleDateString()}</td>
                      <td className="px-4 py-3">
                        <div className="flex flex-wrap items-center justify-end gap-2">
                          {!isSuperAdmin && !isMe && isSuperAdminAccount && (
                            <Button size="sm" variant="danger" isLoading={busy} onClick={() => setToRemove(admin)}>
                              Remove
                            </Button>
                          )}
                          {!isSuperAdmin && !isMe && !isSuperAdminAccount && !hasDeletionRequest && (
                            <Button
                              size="sm"
                              variant="danger"
                              isLoading={busy}
                              onClick={() => handleRequestDeletion(admin)}
                              title="Ask the Super Admin to remove this administrator"
                            >
                              Request removal
                            </Button>
                          )}
                          {isSuperAdmin && <span className="text-xs text-gray-500">Protected</span>}
                        </div>
                      </td>
                    </tr>
                  )
                })}
              </tbody>
            </table>
          </div>

          {/* Deletion Requests Panel (Super Admin Only) */}
          {isSuperAdminAccount && (
            <DeletionRequestsPanel requests={pendingRequests} onUpdate={loadAdmins} toast={toast} />
          )}
        </>
      )}

      {/* Create Admin Dialog */}
      {showCreateDialog && (
        <CreateAdminDialog
          onClose={() => setShowCreateDialog(false)}
          onSuccess={async () => {
            setShowCreateDialog(false)
            toast.success('Administrator created successfully')
            await loadAdmins()
          }}
          onError={(error) => {
            toast.error(error)
          }}
        />
      )}

      <ConfirmDialog
        isOpen={toRemove !== null}
        title="Remove this administrator?"
        message={`${toRemove?.email ?? ''} is signed out everywhere and can no longer sign in.`}
        confirmText="Remove"
        isDangerous
        onCancel={() => setToRemove(null)}
        onConfirm={async () => {
          const target = toRemove
          setToRemove(null)
          if (!target) return
          setBusyId(target.id)
          try {
            await adminService.deactivateUser(target.id)
            toast.success(`${target.email} was removed.`)
            await loadAdmins()
          } catch (err) {
            toast.error(getErrorMessage(err, 'Could not remove that administrator.'))
          } finally {
            setBusyId(null)
          }
        }}
      />

      {/* Delete Request Confirmation */}
      {toDelete && (
        <DeleteAdminConfirmDialog
          admin={toDelete}
          onClose={() => setToDelete(null)}
          onConfirm={handleConfirmDeletion}
        />
      )}
    </div>
  )
}

function StatusPill({
  status,
  children,
}: {
  status: 'active' | 'inactive' | 'locked' | 'warning'
  children: React.ReactNode
}) {
  const styles = {
    active: 'bg-green-100 text-green-900',
    inactive: 'bg-gray-100 text-gray-700',
    locked: 'bg-red-100 text-red-900',
    warning: 'bg-amber-100 text-amber-900',
  }
  return <span className={`rounded-full px-2 py-0.5 text-xs font-medium ${styles[status]}`}>{children}</span>
}

function DeleteAdminConfirmDialog({
  admin,
  onClose,
  onConfirm,
}: {
  admin: AdminUser
  onClose: () => void
  onConfirm: (reason?: string) => Promise<void>
}) {
  const [reason, setReason] = useState('')
  const [isSubmitting, setIsSubmitting] = useState(false)

  const handleSubmit = async () => {
    setIsSubmitting(true)
    try {
      await onConfirm(reason || undefined)
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <div className="fixed inset-0 flex items-center justify-center bg-black bg-opacity-50 z-50">
      <div className="bg-white rounded-lg shadow-lg p-6 max-w-md w-full mx-4">
        <h2 className="text-lg font-semibold text-gray-900 mb-4">Request Administrator Deletion</h2>
        <p className="text-gray-600 mb-4">
          Submit a deletion request for <strong>{admin.email}</strong>. The Super Admin must approve this request.
        </p>

        <div className="mb-4">
          <label className="block text-sm font-medium text-gray-700 mb-2">Reason (optional)</label>
          <textarea
            value={reason}
            onChange={(e) => setReason(e.target.value)}
            placeholder="Why are you requesting this deletion?"
            className="w-full rounded-lg border border-gray-300 p-2 text-sm focus:border-blue-500 focus:ring-1 focus:ring-blue-500"
            rows={3}
          />
        </div>

        <div className="flex gap-3 justify-end">
          <button
            onClick={onClose}
            disabled={isSubmitting}
            className="px-4 py-2 text-sm font-medium text-gray-700 bg-gray-100 rounded-lg hover:bg-gray-200 disabled:opacity-50"
          >
            Cancel
          </button>
          <button
            onClick={handleSubmit}
            disabled={isSubmitting}
            className="px-4 py-2 text-sm font-medium text-white bg-red-600 rounded-lg hover:bg-red-700 disabled:opacity-50"
          >
            {isSubmitting ? 'Submitting...' : 'Submit Request'}
          </button>
        </div>
      </div>
    </div>
  )
}

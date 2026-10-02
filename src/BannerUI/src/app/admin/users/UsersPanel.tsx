'use client'

import React, { useCallback, useEffect, useState } from 'react'
import Link from 'next/link'
import { useSearchParams } from 'next/navigation'
import { Button, ConfirmDialog, Input, Toast } from '@/components/Common'
import { Pagination } from '@/components/admin/Pagination'
import { useDebounce } from '@/hooks/useDebounce'
import { useToast } from '@/hooks/useToast'
import { useAuth } from '@/context/AuthContext'
import { adminService } from '@/api/adminService'
import { getErrorMessage } from '@/api/client'
import { AdminUser, Page } from '@/types/admin'

const PAGE_SIZE = 25

export function UsersPanel() {
  const params = useSearchParams()
  const shopId = params?.get('shopId') ?? ''
  const { user: me } = useAuth()
  const toast = useToast()

  const [search, setSearch] = useState('')
  const [includeInactive, setIncludeInactive] = useState(false)
  const [page, setPage] = useState(1)
  const [result, setResult] = useState<Page<AdminUser> | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [busyId, setBusyId] = useState<string | null>(null)
  const [toDeactivate, setToDeactivate] = useState<AdminUser | null>(null)

  const term = useDebounce(search, 300)

  const load = useCallback(async () => {
    try {
      setResult(await adminService.listUsers({ search: term, shopId, includeInactive, page, pageSize: PAGE_SIZE }))
      setError(null)
    } catch (err) {
      setError(getErrorMessage(err, 'Could not load users.'))
    }
  }, [term, shopId, includeInactive, page])

  useEffect(() => {
    load()
  }, [load])

  // A new search or filter starts again from the first page
  useEffect(() => {
    setPage(1)
  }, [term, includeInactive, shopId])

  const act = async (user: AdminUser, work: () => Promise<unknown>, done: string) => {
    setBusyId(user.id)
    try {
      await work()
      toast.success(done)
      await load()
    } catch (err) {
      toast.error(getErrorMessage(err, 'That did not work.'))
    } finally {
      setBusyId(null)
    }
  }

  return (
    <div className="space-y-6">
      <Toast messages={toast.messages} onRemove={toast.remove} />

      <header>
        <h1 className="text-2xl font-semibold text-gray-900">Users</h1>
        <p className="mt-1 text-gray-600">
          Every login on the platform.
          {shopId && (
            <>
              {' '}
              Showing one shop only.{' '}
              <Link href="/admin/users" className="text-blue-600 hover:text-blue-800">
                Show all
              </Link>
            </>
          )}
        </p>
      </header>

      <div className="flex flex-wrap items-end gap-4">
        <div className="w-full max-w-sm">
          <Input id="user-search" label="Search" placeholder="Name or email" value={search} onChange={(e) => setSearch(e.target.value)} />
        </div>
        <label className="flex items-center gap-2 pb-2 text-sm text-gray-700">
          <input type="checkbox" checked={includeInactive} onChange={(e) => setIncludeInactive(e.target.checked)} />
          Include deactivated
        </label>
      </div>

      {error && (
        <p role="alert" className="text-red-700">
          {error}
        </p>
      )}

      {!result && !error && <p className="text-gray-500">Loading users…</p>}

      {result && (
        <>
          <div className="overflow-x-auto rounded-xl border border-gray-200 bg-white">
            <table className="min-w-full divide-y divide-gray-200 text-sm">
              <thead className="bg-gray-50 text-left text-gray-600">
                <tr>
                  <th className="px-4 py-3 font-medium">User</th>
                  <th className="px-4 py-3 font-medium">Role</th>
                  <th className="px-4 py-3 font-medium">Status</th>
                  <th className="px-4 py-3 font-medium">Joined</th>
                  <th className="px-4 py-3 font-medium text-right">Actions</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-100">
                {result.items.length === 0 && (
                  <tr>
                    <td colSpan={5} className="px-4 py-8 text-center text-gray-500">
                      No users match.
                    </td>
                  </tr>
                )}
                {result.items.map((user) => {
                  const isMe = user.id.toLowerCase() === me?.id.toLowerCase()
                  const busy = busyId === user.id
                  return (
                    <tr key={user.id} data-testid={`user-${user.email}`}>
                      <td className="px-4 py-3">
                        <p className="font-medium text-gray-900">{user.fullName || user.email}</p>
                        <p className="text-gray-600">{user.email}</p>
                      </td>
                      <td className="px-4 py-3 text-gray-700">{user.roles.join(', ') || '—'}</td>
                      <td className="px-4 py-3">
                        <div className="flex flex-wrap gap-1">
                          <Pill tone={user.isActive ? 'green' : 'gray'}>{user.isActive ? 'Active' : 'Deactivated'}</Pill>
                          {user.isLockedOut && <Pill tone="red">Locked out</Pill>}
                          {!user.emailVerified && <Pill tone="amber">Email not verified</Pill>}
                        </div>
                      </td>
                      <td className="px-4 py-3 text-gray-700">{new Date(user.createdAt).toLocaleDateString()}</td>
                      <td className="px-4 py-3">
                        <div className="flex flex-wrap items-center justify-end gap-2">
                          <Link href={`/admin/audit-log?userId=${encodeURIComponent(user.id)}`} className="text-blue-600 hover:text-blue-800">
                            Activity
                          </Link>
                          {user.isLockedOut && (
                            <Button size="sm" variant="secondary" isLoading={busy} onClick={() => act(user, () => adminService.unlockUser(user.id), `${user.email} was unlocked.`)}>
                              Unlock
                            </Button>
                          )}
                          {user.isActive ? (
                            <Button size="sm" variant="danger" disabled={isMe || busy} title={isMe ? 'You cannot deactivate yourself' : undefined} onClick={() => setToDeactivate(user)}>
                              Deactivate
                            </Button>
                          ) : (
                            <Button size="sm" isLoading={busy} onClick={() => act(user, () => adminService.activateUser(user.id), `${user.email} was reactivated.`)}>
                              Reactivate
                            </Button>
                          )}
                        </div>
                      </td>
                    </tr>
                  )
                })}
              </tbody>
            </table>
          </div>

          <Pagination page={result.page} pageSize={result.pageSize} total={result.total} onChange={setPage} />
        </>
      )}

      <ConfirmDialog
        isOpen={toDeactivate !== null}
        title="Deactivate this user?"
        message={`${toDeactivate?.email ?? ''} will be signed out everywhere and cannot sign in until you reactivate them.`}
        confirmText="Deactivate"
        isDangerous
        onCancel={() => setToDeactivate(null)}
        onConfirm={async () => {
          const target = toDeactivate
          setToDeactivate(null)
          if (target) await act(target, () => adminService.deactivateUser(target.id), `${target.email} was deactivated.`)
        }}
      />
    </div>
  )
}

function Pill({ tone, children }: { tone: 'green' | 'gray' | 'red' | 'amber'; children: React.ReactNode }) {
  const styles = {
    green: 'bg-green-100 text-green-800',
    gray: 'bg-gray-100 text-gray-700',
    red: 'bg-red-100 text-red-800',
    amber: 'bg-amber-100 text-amber-800',
  }
  return <span className={`rounded-full px-2 py-0.5 text-xs font-medium ${styles[tone]}`}>{children}</span>
}

'use client'

import React, { FormEvent, useCallback, useEffect, useState } from 'react'
import Link from 'next/link'
import { AppShell } from '@/components/layout/AppShell'
import { Button, Input, Toast } from '@/components/Common'
import { useAuth } from '@/context/AuthContext'
import { useToast } from '@/hooks/useToast'
import { bannerListService, workflowService } from '@/api/workflowService'
import { getErrorMessage } from '@/api/client'
import { Roles } from '@/lib/session'
import { BannerSummary, PublishWorkflow } from '@/types/workflow'
import { StatusBadge } from '../approvals/StatusBadge'
import { SchedulePicker } from './SchedulePicker'

export default function BannersPage() {
  return (
    <AppShell roles={[Roles.ShopOwner, Roles.SalesExecutive]}>
      <Banners />
    </AppShell>
  )
}

function Banners() {
  const { user } = useAuth()
  const shopId = user?.shopId ?? null
  const toast = useToast()

  const [banners, setBanners] = useState<BannerSummary[] | null>(null)
  const [workflows, setWorkflows] = useState<Record<string, PublishWorkflow>>({})
  const [loadError, setLoadError] = useState<string | null>(null)
  const [form, setForm] = useState({ name: '', width: '1200', height: '600' })
  const [formError, setFormError] = useState<string | null>(null)
  const [creating, setCreating] = useState(false)
  const [busyId, setBusyId] = useState<string | null>(null)

  const load = useCallback(async () => {
    if (!shopId) return
    try {
      const [list, flows] = await Promise.all([bannerListService.list(), workflowService.listShopWorkflows(shopId)])
      setBanners(list)
      // newest workflow per banner
      const byBanner: Record<string, PublishWorkflow> = {}
      for (const flow of [...flows].sort((a, b) => a.updatedAt.localeCompare(b.updatedAt))) byBanner[flow.bannerId] = flow
      setWorkflows(byBanner)
      setLoadError(null)
    } catch (err) {
      setLoadError(getErrorMessage(err, 'Could not load your banners.'))
    }
  }, [shopId])

  useEffect(() => {
    load()
  }, [load])

  const create = async (event: FormEvent) => {
    event.preventDefault()
    setFormError(null)

    const width = Number(form.width)
    const height = Number(form.height)
    if (!form.name.trim()) return setFormError('Give the banner a name.')
    if (!Number.isInteger(width) || !Number.isInteger(height) || width < 1 || height < 1 || width > 5000 || height > 5000) {
      return setFormError('Width and height must be whole numbers between 1 and 5000.')
    }

    setCreating(true)
    try {
      await bannerListService.create({ name: form.name.trim(), description: '', width, height })
      setForm({ name: '', width: '1200', height: '600' })
      toast.success('Banner created.')
      await load()
    } catch (err) {
      setFormError(getErrorMessage(err, 'Could not create the banner.'))
    } finally {
      setCreating(false)
    }
  }

  /** New banners get a workflow first; an existing draft, rejected or taken-down one is simply resubmitted. */
  const submitForApproval = async (banner: BannerSummary) => {
    setBusyId(banner.id)
    try {
      const existing = workflows[banner.id]
      const workflow = existing ?? (await workflowService.initiate(banner.id))
      await workflowService.submit(workflow.id)
      toast.success('Submitted for approval.')
      await load()
    } catch (err) {
      toast.error(getErrorMessage(err, 'Could not submit the banner.'))
    } finally {
      setBusyId(null)
    }
  }

  if (!shopId) return <p className="text-gray-600">Your account is not linked to a shop.</p>
  if (loadError) {
    return (
      <p role="alert" className="text-red-700">
        {loadError}
      </p>
    )
  }
  if (!banners) return <p className="text-gray-500">Loading banners…</p>

  return (
    <div className="space-y-8">
      <Toast messages={toast.messages} onRemove={toast.remove} />

      <header>
        <h1 className="text-2xl font-semibold text-gray-900">Banners</h1>
        <p className="mt-1 text-gray-600">Design a banner, then send it for approval. It goes live once approved and published.</p>
      </header>

      <form onSubmit={create} noValidate className="max-w-xl space-y-4 rounded-xl border border-gray-200 bg-white p-5">
        <h2 className="font-semibold text-gray-900">New banner</h2>
        {formError && (
          <div role="alert" className="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-800">
            {formError}
          </div>
        )}
        <Input id="banner-name" label="Name" value={form.name} onChange={(e) => setForm((p) => ({ ...p, name: e.target.value }))} />
        <div className="grid grid-cols-2 gap-3">
          <Input id="banner-width" type="number" label="Width (px)" value={form.width} onChange={(e) => setForm((p) => ({ ...p, width: e.target.value }))} />
          <Input id="banner-height" type="number" label="Height (px)" value={form.height} onChange={(e) => setForm((p) => ({ ...p, height: e.target.value }))} />
        </div>
        <Button type="submit" isLoading={creating}>
          Create banner
        </Button>
      </form>

      {banners.length === 0 ? (
        <p className="text-gray-600">You have no banners yet.</p>
      ) : (
        <ul className="divide-y divide-gray-200 rounded-xl border border-gray-200 bg-white">
          {banners.map((banner) => {
            const workflow = workflows[banner.id]
            const status = workflow?.status
            const canSubmit = !workflow || ['Draft', 'Rejected', 'Unpublished'].includes(status as string)
            const scheduled = !!banner.publishStartAt && !!banner.publishEndAt

            return (
              <li key={banner.id} className="flex flex-wrap items-center justify-between gap-3 px-4 py-3">
                <div>
                  <p className="font-medium text-gray-900">{banner.name}</p>
                  <p className="text-sm text-gray-600">
                    {banner.width} × {banner.height} px · {banner.componentCount} component{banner.componentCount === 1 ? '' : 's'}
                  </p>
                </div>
                <div className="flex flex-wrap items-center gap-3">
                  {status ? <StatusBadge status={status} /> : <span className="text-sm text-gray-500">Not submitted</span>}
                  <Link href={`/banners/${banner.id}/editor`} className="text-sm text-blue-600 hover:text-blue-800">
                    Open editor
                  </Link>
                  {canSubmit && (
                    <Button
                      size="sm"
                      variant="secondary"
                      isLoading={busyId === banner.id}
                      disabled={!scheduled}
                      title={scheduled ? undefined : 'Set when the banner is shown first'}
                      onClick={() => submitForApproval(banner)}
                    >
                      Submit for approval
                    </Button>
                  )}
                </div>
                <div className="w-full border-t border-gray-100 pt-2">
                  <SchedulePicker
                    banner={banner}
                    needsReapprovalOnChange={['PendingApproval', 'Approved', 'Published'].includes(status as string)}
                    onSaved={async (message) => {
                      toast.success(message)
                      await load()
                    }}
                  />
                </div>
              </li>
            )
          })}
        </ul>
      )}
    </div>
  )
}

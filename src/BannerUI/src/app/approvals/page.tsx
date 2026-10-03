'use client'

import React, { useCallback, useEffect, useMemo, useState } from 'react'
import Link from 'next/link'
import { AppShell } from '@/components/layout/AppShell'
import { Button, Toast } from '@/components/Common'
import { useAuth } from '@/context/AuthContext'
import { useToast } from '@/hooks/useToast'
import { bannerListService, workflowService } from '@/api/workflowService'
import { teamService } from '@/api/teamService'
import { getErrorMessage } from '@/api/client'
import { Roles } from '@/lib/session'
import { BannerSummary, PublishWorkflow, WorkflowStatus } from '@/types/workflow'
import { MyApprovalRole } from '@/types/team'
import { formatWindow } from '@/lib/dates'
import { describeDaily } from '@/lib/timeZones'
import { useShopTimeZone } from '@/hooks/useShopTimeZone'
import { ReasonDialog } from './ReasonDialog'
import { StatusBadge } from './StatusBadge'

export default function ApprovalsPage() {
  return (
    <AppShell roles={[Roles.ShopOwner, Roles.SalesExecutive]}>
      <Approvals />
    </AppShell>
  )
}

type Filter = 'attention' | 'live' | 'all'

const FILTER_LABELS: Record<Filter, string> = { attention: 'Needs action', live: 'Live', all: 'All' }

/** Statuses a person still has to act on. */
const NEEDS_ATTENTION: WorkflowStatus[] = ['PendingApproval', 'Approved', 'Draft', 'Rejected', 'Unpublished']

function Approvals() {
  const { user } = useAuth()
  const zone = useShopTimeZone()
  const shopId = user?.shopId ?? null
  const toast = useToast()

  const [workflows, setWorkflows] = useState<PublishWorkflow[] | null>(null)
  const [banners, setBanners] = useState<Record<string, BannerSummary>>({})
  const [role, setRole] = useState<MyApprovalRole | null>(null)
  const [loadError, setLoadError] = useState<string | null>(null)
  const [filter, setFilter] = useState<Filter>('attention')
  const [busyId, setBusyId] = useState<string | null>(null)
  const [rejecting, setRejecting] = useState<PublishWorkflow | null>(null)
  const [expanded, setExpanded] = useState<string | null>(null)

  const load = useCallback(async () => {
    if (!shopId) return
    try {
      const [list, bannerList, myRole] = await Promise.all([
        workflowService.listShopWorkflows(shopId),
        bannerListService.list(),
        teamService.getMyRole(shopId),
      ])
      setWorkflows([...list].sort((a, b) => b.updatedAt.localeCompare(a.updatedAt)))
      setBanners(Object.fromEntries(bannerList.map((b) => [b.id, b])))
      setRole(myRole)
      setLoadError(null)
    } catch (err) {
      setLoadError(getErrorMessage(err, 'Could not load approvals.'))
    }
  }, [shopId])

  useEffect(() => {
    load()
  }, [load])

  const visible = useMemo(
    () =>
      (workflows ?? []).filter((w) =>
        filter === 'all' ? true : filter === 'live' ? w.status === 'Published' : NEEDS_ATTENTION.includes(w.status)
      ),
    [workflows, filter]
  )

  const run = async (workflow: PublishWorkflow, action: () => Promise<unknown>, done: string) => {
    setBusyId(workflow.id)
    try {
      await action()
      toast.success(done)
      await load()
    } catch (err) {
      toast.error(getErrorMessage(err, 'That did not work.'))
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
  if (!workflows || !role) return <p className="text-gray-500">Loading approvals…</p>

  const nameOf = (w: PublishWorkflow) => banners[w.bannerId]?.name ?? 'Banner'

  return (
    <div className="space-y-6">
      <Toast messages={toast.messages} onRemove={toast.remove} />

      <header className="flex flex-wrap items-end justify-between gap-3">
        <div>
          <h1 className="text-2xl font-semibold text-gray-900">Approvals</h1>
          <p className="mt-1 text-gray-600">
            {role.canApprove
              ? 'You can approve or reject banners submitted for this shop.'
              : 'Banners you submit wait here until an approver decides. You cannot approve them yourself.'}
          </p>
        </div>
        <div role="tablist" aria-label="Filter" className="flex gap-1 rounded-lg bg-gray-100 p-1">
          {(['attention', 'live', 'all'] as Filter[]).map((value) => (
            <button
              key={value}
              role="tab"
              aria-selected={filter === value}
              onClick={() => setFilter(value)}
              className={`rounded-md px-3 py-1 text-sm ${filter === value ? 'bg-white font-medium shadow-sm' : 'text-gray-600'}`}
            >
              {FILTER_LABELS[value]}
            </button>
          ))}
        </div>
      </header>

      {visible.length === 0 ? (
        <p className="rounded-xl border border-dashed border-gray-300 bg-white p-8 text-center text-gray-600">
          {filter === 'attention'
            ? 'Nothing is waiting for action.'
            : filter === 'live'
              ? 'No banner is live right now.'
              : 'No banners have been submitted yet.'}{' '}
          <Link href="/banners" className="text-blue-600 hover:text-blue-800">
            Go to banners
          </Link>
        </p>
      ) : (
        <ul className="space-y-4">
          {visible.map((workflow) => {
            const busy = busyId === workflow.id
            const pending = workflow.status === 'PendingApproval'
            const mayDecide = pending && role.canApprove
            const mayPublish = workflow.status === 'Approved' && (role.canApprove || role.isOwner)
            const mayUnpublish = workflow.status === 'Published' && (role.canApprove || role.isOwner)
            const mayResubmit = ['Draft', 'Rejected', 'Unpublished'].includes(workflow.status)

            return (
              <li key={workflow.id} className="rounded-xl border border-gray-200 bg-white p-5 shadow-sm">
                <div className="flex flex-wrap items-start justify-between gap-3">
                  <div>
                    <h2 className="font-semibold text-gray-900">{nameOf(workflow)}</h2>
                    <p className="text-sm text-gray-600">
                      Submitted {new Date(workflow.submittedAt).toLocaleString()}
                    </p>
                    <p className="text-sm text-gray-700" data-testid="approval-schedule">
                      Shown: {formatWindow(banners[workflow.bannerId]?.publishStartAt, banners[workflow.bannerId]?.publishEndAt, zone.timeZoneId)}{banners[workflow.bannerId]?.dailyStartMinutes != null && ` · ${describeDaily(banners[workflow.bannerId]?.dailyStartMinutes, banners[workflow.bannerId]?.dailyEndMinutes, banners[workflow.bannerId]?.activeDays)}`}
                    </p>
                  </div>
                  <StatusBadge status={workflow.status} />
                </div>

                {workflow.status === 'Rejected' && workflow.rejectionReason && (
                  <p className="mt-3 rounded-lg bg-red-50 px-3 py-2 text-sm text-red-800">
                    Rejected: {workflow.rejectionReason}
                  </p>
                )}
                {pending && !role.canApprove && (
                  <p className="mt-3 text-sm text-gray-600">Waiting for an approver.</p>
                )}

                <div className="mt-4 flex flex-wrap items-center gap-2">
                  {mayDecide && (
                    <>
                      <Button size="sm" isLoading={busy} onClick={() => run(workflow, () => workflowService.approve(workflow.id), 'Banner approved.')}>
                        Approve
                      </Button>
                      <Button size="sm" variant="danger" disabled={busy} onClick={() => setRejecting(workflow)}>
                        Reject
                      </Button>
                    </>
                  )}
                  {mayPublish && (
                    <Button size="sm" isLoading={busy} onClick={() => run(workflow, () => workflowService.publish(workflow.id), 'Banner published.')}>
                      Publish
                    </Button>
                  )}
                  {mayUnpublish && (
                    <Button size="sm" variant="secondary" isLoading={busy} onClick={() => run(workflow, () => workflowService.unpublish(workflow.id), 'Banner unpublished.')}>
                      Unpublish
                    </Button>
                  )}
                  {mayResubmit && (
                    <Button size="sm" variant="secondary" isLoading={busy} onClick={() => run(workflow, () => workflowService.submit(workflow.id), 'Submitted for approval.')}>
                      Submit for approval
                    </Button>
                  )}
                  <Link href={`/banners/${workflow.bannerId}/editor`} className="text-sm text-blue-600 hover:text-blue-800">
                    Open in editor
                  </Link>
                  <button
                    type="button"
                    className="ml-auto text-sm text-gray-600 hover:text-gray-900"
                    aria-expanded={expanded === workflow.id}
                    onClick={() => setExpanded(expanded === workflow.id ? null : workflow.id)}
                  >
                    {expanded === workflow.id ? 'Hide history' : 'History'}
                  </button>
                </div>

                {expanded === workflow.id && (
                  <ol className="mt-4 space-y-2 border-t border-gray-100 pt-3 text-sm">
                    {workflow.events.length === 0 && <li className="text-gray-500">No history yet.</li>}
                    {workflow.events.map((event, index) => (
                      <li key={index} className="flex flex-wrap gap-x-3 text-gray-700">
                        <span className="text-gray-500">{new Date(event.occurredAt).toLocaleString()}</span>
                        <span>{event.outcome ?? event.eventType}</span>
                        <span className="text-gray-500">by {event.actorName}</span>
                        {event.comment && <span className="italic">“{event.comment}”</span>}
                      </li>
                    ))}
                  </ol>
                )}
              </li>
            )
          })}
        </ul>
      )}

      <ReasonDialog
        isOpen={rejecting !== null}
        title="Reject this banner"
        label="Reason (shown to the person who submitted it)"
        confirmText="Reject banner"
        onCancel={() => setRejecting(null)}
        onConfirm={async (reason) => {
          const target = rejecting
          setRejecting(null)
          if (target) await run(target, () => workflowService.reject(target.id, reason), 'Banner rejected.')
        }}
      />
    </div>
  )
}

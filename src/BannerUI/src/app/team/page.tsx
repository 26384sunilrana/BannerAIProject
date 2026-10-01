'use client'

import React, { FormEvent, useCallback, useEffect, useState } from 'react'
import Link from 'next/link'
import { AppShell } from '@/components/layout/AppShell'
import { Button, ConfirmDialog, Input, Toast } from '@/components/Common'
import { useAuth } from '@/context/AuthContext'
import { useToast } from '@/hooks/useToast'
import { teamService } from '@/api/teamService'
import { subscriptionService } from '@/api/subscriptionService'
import { SUBSCRIBED_STATUSES } from '@/lib/pricing'
import { getErrorMessage } from '@/api/client'
import { Roles } from '@/lib/session'
import { ShopTeam, TeamMember } from '@/types/team'

export default function TeamPage() {
  return (
    <AppShell roles={[Roles.ShopOwner]}>
      <TeamManager />
    </AppShell>
  )
}

const EMPTY_FORM = { firstName: '', lastName: '', email: '', password: '' }

function TeamManager() {
  const { user } = useAuth()
  const shopId = user?.shopId ?? null
  const toast = useToast()

  const [team, setTeam] = useState<ShopTeam | null>(null)
  // Logins can only be added once the shop has a subscription
  const [subscribed, setSubscribed] = useState<boolean | null>(null)
  const [loadError, setLoadError] = useState<string | null>(null)
  const [form, setForm] = useState(EMPTY_FORM)
  const [formError, setFormError] = useState<string | null>(null)
  const [adding, setAdding] = useState(false)
  const [toRemove, setToRemove] = useState<TeamMember | null>(null)
  const [removing, setRemoving] = useState(false)

  // Approver choices being edited, kept apart from the saved ones until the owner saves
  const [ownerApproves, setOwnerApproves] = useState(true)
  const [approverIds, setApproverIds] = useState<string[]>([])
  const [savingApprovers, setSavingApprovers] = useState(false)

  const applyTeam = useCallback((loaded: ShopTeam) => {
    setTeam(loaded)
    setOwnerApproves(loaded.ownerIsApprover)
    setApproverIds(loaded.salesExecutives.filter((m) => m.isApprover).map((m) => m.userId))
  }, [])

  useEffect(() => {
    if (!shopId) return
    teamService
      .getTeam(shopId)
      .then(applyTeam)
      .catch((err) => setLoadError(getErrorMessage(err, 'Could not load your team.')))
    subscriptionService
      .getCurrent(shopId)
      .then((s) => setSubscribed(!!s && SUBSCRIBED_STATUSES.includes(s.status)))
      .catch(() => setSubscribed(true)) // the server still enforces it
  }, [shopId, applyTeam])

  if (!shopId) {
    return <p className="text-gray-600">Your account is not linked to a shop.</p>
  }
  if (loadError) {
    return (
      <p role="alert" className="text-red-700">
        {loadError}
      </p>
    )
  }
  if (!team) {
    return <p className="text-gray-500">Loading team…</p>
  }

  const full = team.salesExecutives.length >= team.maxSalesExecutives
  const approversChanged =
    ownerApproves !== team.ownerIsApprover ||
    [...approverIds].sort().join() !== team.salesExecutives.filter((m) => m.isApprover).map((m) => m.userId).sort().join()
  const noApprover = !ownerApproves && approverIds.length === 0

  const setField = (field: keyof typeof EMPTY_FORM) => (e: React.ChangeEvent<HTMLInputElement>) =>
    setForm((prev) => ({ ...prev, [field]: e.target.value }))

  const addExecutive = async (event: FormEvent) => {
    event.preventDefault()
    setFormError(null)

    if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(form.email.trim())) {
      setFormError('Enter a valid email address.')
      return
    }
    if (form.password.length < 8) {
      setFormError('The password needs at least 8 characters.')
      return
    }

    setAdding(true)
    try {
      await teamService.addSalesExecutive(shopId, {
        email: form.email.trim(),
        password: form.password,
        firstName: form.firstName.trim() || undefined,
        lastName: form.lastName.trim() || undefined,
      })
      setForm(EMPTY_FORM)
      applyTeam(await teamService.getTeam(shopId))
      toast.success('Sales executive added.')
    } catch (err) {
      setFormError(getErrorMessage(err, 'Could not add the login.'))
    } finally {
      setAdding(false)
    }
  }

  const confirmRemove = async () => {
    if (!toRemove) return
    setRemoving(true)
    try {
      await teamService.removeSalesExecutive(shopId, toRemove.userId)
      applyTeam(await teamService.getTeam(shopId))
      toast.success(`${toRemove.fullName || toRemove.email} was removed. You can add another login.`)
    } catch (err) {
      toast.error(getErrorMessage(err, 'Could not remove the login.'))
    } finally {
      setRemoving(false)
      setToRemove(null)
    }
  }

  const saveApprovers = async () => {
    setSavingApprovers(true)
    try {
      applyTeam(await teamService.setApprovers(shopId, ownerApproves, approverIds))
      toast.success('Approvers saved.')
    } catch (err) {
      toast.error(getErrorMessage(err, 'Could not save the approvers.'))
    } finally {
      setSavingApprovers(false)
    }
  }

  const toggleApprover = (id: string) =>
    setApproverIds((prev) => (prev.includes(id) ? prev.filter((x) => x !== id) : [...prev, id]))

  return (
    <div className="space-y-10">
      <Toast messages={toast.messages} onRemove={toast.remove} />

      <header>
        <h1 className="text-2xl font-semibold text-gray-900">Team</h1>
        <p className="mt-1 text-gray-600">
          Your shop can have up to {team.maxSalesExecutives} sales executive logins. Remove one to add another.
        </p>
      </header>

      <section aria-labelledby="executives-heading">
        <h2 id="executives-heading" className="text-lg font-semibold text-gray-900">
          Sales executives ({team.salesExecutives.length} of {team.maxSalesExecutives})
        </h2>

        {team.salesExecutives.length === 0 ? (
          <p className="mt-3 text-gray-600">No sales executives yet.</p>
        ) : (
          <ul className="mt-3 divide-y divide-gray-200 rounded-xl border border-gray-200 bg-white">
            {team.salesExecutives.map((member) => (
              <li key={member.userId} className="flex flex-wrap items-center justify-between gap-3 px-4 py-3">
                <div>
                  <p className="font-medium text-gray-900">{member.fullName || member.email}</p>
                  <p className="text-sm text-gray-600">{member.email}</p>
                </div>
                <div className="flex items-center gap-3">
                  {member.isApprover && (
                    <span className="rounded-full bg-green-100 px-2 py-0.5 text-xs font-medium text-green-800">Approver</span>
                  )}
                  <Button variant="danger" size="sm" onClick={() => setToRemove(member)}>
                    Remove
                  </Button>
                </div>
              </li>
            ))}
          </ul>
        )}
      </section>

      <section aria-labelledby="add-heading">
        <h2 id="add-heading" className="text-lg font-semibold text-gray-900">
          Add a sales executive
        </h2>
        {subscribed === false ? (
          <p className="mt-3 rounded-xl border border-yellow-200 bg-yellow-50 p-4 text-yellow-900">
            You need a subscription before you can add logins.{' '}
            <Link href="/subscription" className="font-medium text-blue-700 hover:text-blue-900">
              Choose a plan
            </Link>
          </p>
        ) : full ? (
          <p className="mt-3 text-gray-600">
            You have reached the limit of {team.maxSalesExecutives} logins. Remove one to add another.
          </p>
        ) : (
          <form onSubmit={addExecutive} noValidate className="mt-3 max-w-xl space-y-4 rounded-xl border border-gray-200 bg-white p-5">
            {formError && (
              <div role="alert" className="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-800">
                {formError}
              </div>
            )}
            <div className="grid grid-cols-2 gap-3">
              <Input id="exec-first" label="First name" value={form.firstName} onChange={setField('firstName')} />
              <Input id="exec-last" label="Last name" value={form.lastName} onChange={setField('lastName')} />
            </div>
            <Input id="exec-email" type="email" label="Email" autoComplete="off" value={form.email} onChange={setField('email')} />
            <Input
              id="exec-password"
              type="password"
              label="Temporary password"
              autoComplete="new-password"
              helperText="At least 8 characters. Share it with them securely."
              value={form.password}
              onChange={setField('password')}
            />
            <Button type="submit" isLoading={adding}>
              Add login
            </Button>
          </form>
        )}
      </section>

      <section aria-labelledby="approvers-heading">
        <h2 id="approvers-heading" className="text-lg font-semibold text-gray-900">
          Who approves new and changed banners
        </h2>
        <p className="mt-1 text-sm text-gray-600">
          Every new banner, schedule change and restored version needs approval before it goes live.
        </p>

        <fieldset className="mt-3 max-w-xl space-y-3 rounded-xl border border-gray-200 bg-white p-5">
          <legend className="sr-only">Approvers</legend>
          <label className="flex items-center gap-3">
            <input type="checkbox" checked={ownerApproves} onChange={(e) => setOwnerApproves(e.target.checked)} />
            <span className="text-gray-900">I approve banners (shop owner)</span>
          </label>
          {team.salesExecutives.map((member) => (
            <label key={member.userId} className="flex items-center gap-3">
              <input
                type="checkbox"
                checked={approverIds.includes(member.userId)}
                onChange={() => toggleApprover(member.userId)}
              />
              <span className="text-gray-900">{member.fullName || member.email}</span>
            </label>
          ))}

          {noApprover && (
            <p role="alert" className="text-sm text-red-700">
              Choose at least one approver.
            </p>
          )}
          <Button onClick={saveApprovers} isLoading={savingApprovers} disabled={!approversChanged || noApprover}>
            Save approvers
          </Button>
        </fieldset>
      </section>

      <ConfirmDialog
        isOpen={toRemove !== null}
        title="Remove this login?"
        message={`${toRemove?.fullName || toRemove?.email || 'This person'} will no longer be able to sign in. You can add another login afterwards.`}
        confirmText="Remove"
        isDangerous
        isLoading={removing}
        onConfirm={confirmRemove}
        onCancel={() => setToRemove(null)}
      />
    </div>
  )
}

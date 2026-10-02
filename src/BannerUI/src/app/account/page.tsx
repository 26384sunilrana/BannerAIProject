'use client'

import React, { FormEvent, useEffect, useState } from 'react'
import { AppShell } from '@/components/layout/AppShell'
import { Button, ConfirmDialog, Input, Toast } from '@/components/Common'
import { useAuth } from '@/context/AuthContext'
import { useToast } from '@/hooks/useToast'
import { accountService, Account } from '@/api/accountService'
import { getErrorMessage } from '@/api/client'

export default function AccountPage() {
  return (
    <AppShell>
      <AccountSettings />
    </AppShell>
  )
}

const ROLE_NAMES: Record<string, string> = {
  Admin: 'Administrator',
  ShopOwner: 'Shop owner',
  SalesExecutive: 'Sales executive',
}

function AccountSettings() {
  const { logout } = useAuth()
  const toast = useToast()

  const [account, setAccount] = useState<Account | null>(null)
  const [loadError, setLoadError] = useState<string | null>(null)

  const [firstName, setFirstName] = useState('')
  const [lastName, setLastName] = useState('')
  const [phone, setPhone] = useState('')
  const [detailsError, setDetailsError] = useState<string | null>(null)
  const [savingDetails, setSavingDetails] = useState(false)

  const [passwords, setPasswords] = useState({ currentPassword: '', newPassword: '', confirmPassword: '' })
  const [passwordError, setPasswordError] = useState<string | null>(null)
  const [savingPassword, setSavingPassword] = useState(false)

  const [confirmSignOut, setConfirmSignOut] = useState(false)
  const [signingOut, setSigningOut] = useState(false)

  const apply = (loaded: Account) => {
    setAccount(loaded)
    setFirstName(loaded.firstName)
    setLastName(loaded.lastName)
    setPhone(loaded.phoneNumber ?? '')
  }

  useEffect(() => {
    accountService
      .get()
      .then(apply)
      .catch((err) => setLoadError(getErrorMessage(err, 'Could not load your account.')))
  }, [])

  if (loadError) {
    return (
      <p role="alert" className="text-red-700">
        {loadError}
      </p>
    )
  }
  if (!account) return <p className="text-gray-500">Loading your account…</p>

  const saveDetails = async (event: FormEvent) => {
    event.preventDefault()
    setDetailsError(null)
    if (!firstName.trim()) {
      setDetailsError('Enter your first name.')
      return
    }

    setSavingDetails(true)
    try {
      apply(await accountService.update({ firstName, lastName, phoneNumber: phone.trim() || null }))
      toast.success('Your details were saved.')
    } catch (err) {
      setDetailsError(getErrorMessage(err, 'Your details could not be saved.'))
    } finally {
      setSavingDetails(false)
    }
  }

  const changePassword = async (event: FormEvent) => {
    event.preventDefault()
    setPasswordError(null)
    if (!passwords.currentPassword) {
      setPasswordError('Enter your current password.')
      return
    }
    if (passwords.newPassword.length < 8) {
      setPasswordError('The new password needs at least 8 characters.')
      return
    }
    if (passwords.newPassword !== passwords.confirmPassword) {
      setPasswordError('The two new passwords are not the same.')
      return
    }

    setSavingPassword(true)
    try {
      await accountService.changePassword(passwords)
      setPasswords({ currentPassword: '', newPassword: '', confirmPassword: '' })
      toast.success('Your password was changed. Your other devices were signed out.')
    } catch (err) {
      setPasswordError(getErrorMessage(err, 'The password could not be changed.'))
    } finally {
      setSavingPassword(false)
    }
  }

  const signOutEverywhere = async () => {
    setSigningOut(true)
    try {
      await accountService.signOutEverywhere()
    } catch {
      // the local session is cleared either way
    }
    await logout('/login')
  }

  const setPassword = (field: keyof typeof passwords) => (e: React.ChangeEvent<HTMLInputElement>) =>
    setPasswords((prev) => ({ ...prev, [field]: e.target.value }))

  return (
    <div className="max-w-2xl space-y-10">
      <Toast messages={toast.messages} onRemove={toast.remove} />

      <header>
        <h1 className="text-2xl font-semibold text-gray-900">My account</h1>
        <p className="mt-1 text-gray-600">
          {account.email} · {account.roles.map((r) => ROLE_NAMES[r] ?? r).join(', ') || 'No role'}
        </p>
      </header>

      <section aria-labelledby="details-heading">
        <h2 id="details-heading" className="text-lg font-semibold text-gray-900">
          Your details
        </h2>
        <form onSubmit={saveDetails} noValidate className="mt-3 space-y-4 rounded-xl border border-gray-200 bg-white p-5">
          {detailsError && (
            <div role="alert" className="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-800">
              {detailsError}
            </div>
          )}
          <Input id="account-email" label="Email" value={account.email} readOnly disabled helperText="Your email is your sign-in name and cannot be changed here." />
          <div className="grid gap-3 sm:grid-cols-2">
            <Input id="account-first" label="First name" value={firstName} onChange={(e) => setFirstName(e.target.value)} autoComplete="given-name" />
            <Input id="account-last" label="Last name" value={lastName} onChange={(e) => setLastName(e.target.value)} autoComplete="family-name" />
          </div>
          <Input id="account-phone" label="Phone" value={phone} onChange={(e) => setPhone(e.target.value)} autoComplete="tel" />
          <Button type="submit" isLoading={savingDetails}>
            Save details
          </Button>
        </form>
      </section>

      <section aria-labelledby="password-heading">
        <h2 id="password-heading" className="text-lg font-semibold text-gray-900">
          Change password
        </h2>
        <form onSubmit={changePassword} noValidate className="mt-3 space-y-4 rounded-xl border border-gray-200 bg-white p-5">
          {passwordError && (
            <div role="alert" className="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-800">
              {passwordError}
            </div>
          )}
          <Input id="current-password" type="password" label="Current password" autoComplete="current-password" value={passwords.currentPassword} onChange={setPassword('currentPassword')} />
          <Input id="new-password" type="password" label="New password" autoComplete="new-password" helperText="At least 8 characters." value={passwords.newPassword} onChange={setPassword('newPassword')} />
          <Input id="confirm-password" type="password" label="New password again" autoComplete="new-password" value={passwords.confirmPassword} onChange={setPassword('confirmPassword')} />
          <Button type="submit" isLoading={savingPassword}>
            Change password
          </Button>
        </form>
      </section>

      <section aria-labelledby="sessions-heading">
        <h2 id="sessions-heading" className="text-lg font-semibold text-gray-900">
          Signed-in devices
        </h2>
        <p className="mt-1 text-sm text-gray-600">
          Lost a phone or used a shared computer? Sign out everywhere. Every device, this one included, has to sign in again. A device that is
          already open can keep working for up to 15 minutes before it is asked to.
        </p>
        <div className="mt-3">
          <Button variant="danger" onClick={() => setConfirmSignOut(true)}>
            Sign out everywhere
          </Button>
        </div>
      </section>

      <ConfirmDialog
        isOpen={confirmSignOut}
        title="Sign out everywhere?"
        message="You will be signed out on this device and on every other one."
        confirmText="Sign out everywhere"
        isDangerous
        isLoading={signingOut}
        onCancel={() => setConfirmSignOut(false)}
        onConfirm={signOutEverywhere}
      />
    </div>
  )
}

'use client'

import React, { FormEvent, useEffect, useState } from 'react'
import Link from 'next/link'
import { useSearchParams } from 'next/navigation'
import { AuthCard, FormError } from '@/components/auth/AuthCard'
import { Button, Input } from '@/components/Common'
import { authService } from '@/api/authService'
import { getErrorMessage } from '@/api/client'

export function ResetPasswordForm() {
  const params = useSearchParams()
  // the token is read once and then taken out of the address bar and the history, so it is not left lying around
  const [token] = useState(() => params?.get('token') ?? '')
  const [password, setPassword] = useState('')
  const [confirm, setConfirm] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [done, setDone] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  useEffect(() => {
    if (token && typeof window !== 'undefined') window.history.replaceState(null, '', window.location.pathname)
  }, [token])

  if (!token) {
    return (
      <AuthCard
        title="Choose a new password"
        footer={
          <Link href="/forgot-password" className="text-blue-600 hover:text-blue-800 font-medium">
            Ask for a new link
          </Link>
        }
      >
        <p role="alert" className="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-800">
          This link is not complete. Open the link from your e-mail again, or ask for a new one.
        </p>
      </AuthCard>
    )
  }

  const submit = async (event: FormEvent) => {
    event.preventDefault()
    setError(null)
    if (password.length < 8) {
      setError('The password must be at least 8 characters.')
      return
    }
    if (password !== confirm) {
      setError('The two passwords do not match.')
      return
    }

    setBusy(true)
    try {
      setDone(await authService.resetPassword(token, password, confirm))
    } catch (err) {
      setError(getErrorMessage(err, 'Could not change the password. Ask for a new link and try again.'))
    } finally {
      setBusy(false)
    }
  }

  return (
    <AuthCard
      title="Choose a new password"
      subtitle="You will be signed out everywhere and can then sign in with the new password."
      footer={
        <Link href="/login" className="text-blue-600 hover:text-blue-800 font-medium">
          Back to sign in
        </Link>
      }
    >
      {done ? (
        <div className="space-y-4">
          <p role="status" className="rounded-lg border border-green-200 bg-green-50 px-4 py-3 text-sm text-green-900">
            {done}
          </p>
          <Link href="/login" className="block text-center text-blue-600 hover:text-blue-800 font-medium">
            Sign in
          </Link>
        </div>
      ) : (
        <form onSubmit={submit} noValidate className="space-y-4">
          <FormError message={error} />
          <Input
            id="password"
            name="password"
            type="password"
            label="New password"
            autoComplete="new-password"
            helperText="At least 8 characters"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
          />
          <Input
            id="confirmPassword"
            name="confirmPassword"
            type="password"
            label="Confirm new password"
            autoComplete="new-password"
            value={confirm}
            onChange={(e) => setConfirm(e.target.value)}
          />
          <Button type="submit" isLoading={busy} className="w-full">
            Change password
          </Button>
        </form>
      )}
    </AuthCard>
  )
}

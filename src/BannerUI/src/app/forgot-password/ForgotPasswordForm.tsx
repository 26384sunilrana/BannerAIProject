'use client'

import React, { FormEvent, useState } from 'react'
import Link from 'next/link'
import { AuthCard, FormError } from '@/components/auth/AuthCard'
import { Button, Input } from '@/components/Common'
import { authService } from '@/api/authService'
import { getErrorMessage } from '@/api/client'

export function ForgotPasswordForm() {
  const [email, setEmail] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [sentMessage, setSentMessage] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  const submit = async (event: FormEvent) => {
    event.preventDefault()
    setError(null)
    if (!email.trim()) {
      setError('Enter the e-mail address you sign in with.')
      return
    }

    setBusy(true)
    try {
      // the answer is the same whether or not the address has an account
      setSentMessage(await authService.forgotPassword(email))
    } catch (err) {
      setError(getErrorMessage(err, 'Could not send the link. Please try again.'))
    } finally {
      setBusy(false)
    }
  }

  return (
    <AuthCard
      title="Forgot your password?"
      subtitle="Enter your e-mail address and we will send you a link to choose a new one."
      footer={
        <Link href="/login" className="text-blue-600 hover:text-blue-800 font-medium">
          Back to sign in
        </Link>
      }
    >
      {sentMessage ? (
        <p role="status" className="rounded-lg border border-green-200 bg-green-50 px-4 py-3 text-sm text-green-900">
          {sentMessage}. The link works for one hour.
        </p>
      ) : (
        <form onSubmit={submit} noValidate className="space-y-4">
          <FormError message={error} />
          <Input id="email" name="email" type="email" label="Email" autoComplete="email" value={email} onChange={(e) => setEmail(e.target.value)} />
          <Button type="submit" isLoading={busy} className="w-full">
            Send the link
          </Button>
        </form>
      )}
    </AuthCard>
  )
}

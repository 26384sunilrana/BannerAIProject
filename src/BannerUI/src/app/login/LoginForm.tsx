'use client'

import React, { FormEvent, useState } from 'react'
import Link from 'next/link'
import { useRouter, useSearchParams } from 'next/navigation'
import { AuthCard, FormError } from '@/components/auth/AuthCard'
import { Button, Input } from '@/components/Common'
import { useAuth } from '@/context/AuthContext'
import { getErrorMessage } from '@/api/client'
import { safeReturnUrl } from '@/lib/redirect'

export function LoginForm() {
  const { login } = useAuth()
  const router = useRouter()
  const params = useSearchParams()

  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  const submit = async (event: FormEvent) => {
    event.preventDefault()
    setError(null)

    if (!email.trim() || !password) {
      setError('Enter your email and password.')
      return
    }

    setBusy(true)
    try {
      await login({ email, password })
      router.replace(safeReturnUrl(params?.get('returnUrl')))
    } catch (err) {
      setError(getErrorMessage(err, 'Could not sign in. Please try again.'))
    } finally {
      setBusy(false)
    }
  }

  return (
    <AuthCard
      title="Sign in"
      subtitle="Welcome back. Sign in to manage your banners."
      footer={
        <>
          New shop?{' '}
          <Link href="/register" className="text-blue-600 hover:text-blue-800 font-medium">
            Create an account
          </Link>
        </>
      }
    >
      <form onSubmit={submit} noValidate className="space-y-4">
        {params?.get('handover') === '1' && (
          <p role="status" className="rounded-lg border border-green-200 bg-green-50 px-4 py-3 text-sm text-green-900">
            The shop was handed over. Sign in again with your own login.
          </p>
        )}
        <FormError message={error} />
        <Input
          id="email"
          name="email"
          type="email"
          label="Email"
          autoComplete="email"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
        />
        <Input
          id="password"
          name="password"
          type="password"
          label="Password"
          autoComplete="current-password"
          value={password}
          onChange={(e) => setPassword(e.target.value)}
        />
        <Button type="submit" isLoading={busy} className="w-full">
          Sign in
        </Button>
      </form>
    </AuthCard>
  )
}

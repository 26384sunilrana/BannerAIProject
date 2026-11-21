'use client'

import React, { useEffect, useRef, useState } from 'react'
import Link from 'next/link'
import { useSearchParams } from 'next/navigation'
import { AuthCard } from '@/components/auth/AuthCard'
import { authService } from '@/api/authService'
import { getErrorMessage } from '@/api/client'

type State = { kind: 'working' } | { kind: 'done'; message: string } | { kind: 'failed'; message: string }

export function VerifyEmailView() {
  const params = useSearchParams()
  const [token] = useState(() => params?.get('token') ?? '')
  const [state, setState] = useState<State>(token ? { kind: 'working' } : { kind: 'failed', message: 'This link is not complete. Open the link from your e-mail again.' })
  // a link works once, and React may run an effect twice in development: ask only once
  const asked = useRef(false)

  useEffect(() => {
    if (!token || asked.current) return
    asked.current = true
    if (typeof window !== 'undefined') window.history.replaceState(null, '', window.location.pathname)

    authService
      .verifyEmail(token)
      .then((message) => setState({ kind: 'done', message }))
      .catch((err) => setState({ kind: 'failed', message: getErrorMessage(err, 'This link is not valid or has expired.') }))
  }, [token])

  return (
    <AuthCard
      title="Verify your e-mail"
      footer={
        <Link href="/dashboard" className="text-blue-600 hover:text-blue-800 font-medium">
          Go to Banner AI
        </Link>
      }
    >
      {state.kind === 'working' && <p className="text-gray-600">Checking your link…</p>}
      {state.kind === 'done' && (
        <p role="status" className="rounded-lg border border-green-200 bg-green-50 px-4 py-3 text-sm text-green-900">
          {state.message}
        </p>
      )}
      {state.kind === 'failed' && (
        <p role="alert" className="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-800">
          {state.message} Sign in and use "Send the link again" at the top of the page to get a new one.
        </p>
      )}
    </AuthCard>
  )
}

'use client'

import React, { useEffect, useState } from 'react'
import { authService } from '@/api/authService'
import { getErrorMessage } from '@/api/client'
import { useAuth } from '@/context/AuthContext'

/**
 * A reminder, not a gate (decided 2026-11-21): signing in never needs a verified address. Shown only when this installation can send
 * mail and the signed-in person has not verified yet.
 */
export function VerifyEmailBanner() {
  const { user } = useAuth()
  const [needed, setNeeded] = useState(false)
  const [note, setNote] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)
  const userId = user?.id

  useEffect(() => {
    if (!userId) return
    let alive = true
    authService
      .getEmailStatus()
      .then((status) => {
        if (alive) setNeeded(status.emailEnabled && !status.emailVerified)
      })
      .catch(() => {
        // the banner is a convenience: if the call fails there is simply no banner
      })
    return () => {
      alive = false
    }
  }, [userId])

  if (!needed) return null

  const resend = async () => {
    setBusy(true)
    try {
      setNote(await authService.resendVerification())
    } catch (err) {
      setNote(getErrorMessage(err, 'Could not send the link. Please try again.'))
    } finally {
      setBusy(false)
    }
  }

  return (
    <div role="status" data-testid="verify-email-banner" className="border-b border-amber-200 bg-amber-50 px-6 py-2 text-sm text-amber-900">
      <div className="mx-auto flex max-w-6xl flex-wrap items-center gap-3">
        <span>Please verify your e-mail address. We sent you a link when you signed up.</span>
        <button type="button" onClick={resend} disabled={busy} className="font-medium text-blue-700 hover:text-blue-900 disabled:opacity-60">
          Send the link again
        </button>
        {note && <span>{note}</span>}
      </div>
    </div>
  )
}

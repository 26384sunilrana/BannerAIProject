'use client'

import React from 'react'
import Link from 'next/link'
import { SubscriptionNotice } from '@/lib/subscriptionNotice'
import { Button } from '@/components/Common'

const TONES = {
  info: 'border-blue-200 bg-blue-50 text-blue-900',
  warning: 'border-amber-200 bg-amber-50 text-amber-900',
  danger: 'border-red-200 bg-red-50 text-red-900',
}

interface SubscriptionNoticeCardProps {
  notice: SubscriptionNotice
  /** When given, a Renew now button is shown (if the notice allows renewing). */
  onRenew?: () => void
  renewing?: boolean
  /** When given instead, a link to the subscription page is shown. */
  manageHref?: string
}

export function SubscriptionNoticeCard({ notice, onRenew, renewing, manageHref }: SubscriptionNoticeCardProps) {
  return (
    <div role="status" data-testid="subscription-notice" className={`mt-4 rounded-xl border px-4 py-3 ${TONES[notice.tone]}`}>
      <p className="font-semibold">{notice.title}</p>
      <p className="mt-1 text-sm">{notice.text}</p>
      {notice.canRenew && (
        <div className="mt-3">
          {onRenew ? (
            <Button size="sm" isLoading={renewing} onClick={onRenew}>
              Renew now
            </Button>
          ) : manageHref ? (
            <Link href={manageHref} className="text-sm font-medium underline">
              Renew now
            </Link>
          ) : null}
        </div>
      )}
    </div>
  )
}

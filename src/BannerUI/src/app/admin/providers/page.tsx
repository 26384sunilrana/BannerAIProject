'use client'

import React, { useCallback, useEffect, useState } from 'react'
import { Button, Input, Toast } from '@/components/Common'
import { useToast } from '@/hooks/useToast'
import { ProviderStatus, providerService } from '@/api/providerService'
import { getErrorMessage } from '@/api/client'

const TITLES: Record<ProviderStatus['name'], { title: string; about: string }> = {
  payments: { title: 'Payments', about: 'Owners pay for renewals online. Without a provider the Renew button extends the plan without charging.' },
  sms: { title: 'Text messages (SMS)', about: 'Renewal reminders by text. Without a provider no text is sent.' },
  email: { title: 'E-mail', about: 'Reminders, password reset and e-mail verification. Without a mail server no mail is sent.' },
}

export default function AdminProvidersPage() {
  const toast = useToast()
  const [status, setStatus] = useState<ProviderStatus[] | null>(null)
  const [loadError, setLoadError] = useState<string | null>(null)
  const [emailTo, setEmailTo] = useState('')
  const [smsTo, setSmsTo] = useState('')
  const [busy, setBusy] = useState<'email' | 'sms' | null>(null)

  const load = useCallback(async () => {
    try {
      setStatus(await providerService.getStatus())
      setLoadError(null)
    } catch (err) {
      setLoadError(getErrorMessage(err, 'Could not load the provider status.'))
    }
  }, [])

  useEffect(() => {
    load()
  }, [load])

  const test = async (kind: 'email' | 'sms') => {
    if (kind === 'sms' && !smsTo.trim()) {
      toast.error('Enter the phone number to send the test text to.')
      return
    }
    setBusy(kind)
    try {
      const result = kind === 'email' ? await providerService.sendTestEmail(emailTo) : await providerService.sendTestSms(smsTo)
      if (result.sent) toast.success(kind === 'email' ? 'The test e-mail was sent.' : 'The test text was sent.')
      else toast.error(result.reason ?? 'The test message was not sent.')
    } catch (err) {
      toast.error(getErrorMessage(err, 'The test did not work.'))
    } finally {
      setBusy(null)
    }
  }

  return (
    <div className="space-y-6">
      <Toast messages={toast.messages} onRemove={toast.remove} />

      <header>
        <h1 className="text-2xl font-semibold text-gray-900">Providers</h1>
        <p className="mt-1 text-gray-600">
          Whether payments, text messages and e-mail are connected. Keys and passwords are set on the server, never here, and are never shown.
        </p>
      </header>

      {loadError && (
        <p role="alert" className="text-red-700">
          {loadError}
        </p>
      )}
      {!status && !loadError && <p className="text-gray-500">Loading…</p>}

      <ul className="grid gap-4 md:grid-cols-3">
        {status?.map((item) => (
          <li key={item.name} className="rounded-xl border border-gray-200 bg-white p-5" data-testid={`provider-${item.name}`}>
            <div className="flex items-center justify-between gap-2">
              <h2 className="text-lg font-semibold text-gray-900">{TITLES[item.name]?.title ?? item.name}</h2>
              <span
                className={`rounded-full px-2.5 py-0.5 text-xs font-medium ${item.configured ? 'bg-green-100 text-green-800' : 'bg-gray-100 text-gray-700'}`}
              >
                {item.configured ? 'Connected' : 'Not connected'}
              </span>
            </div>
            <p className="mt-1 text-sm text-gray-600">{TITLES[item.name]?.about}</p>
            <p className="mt-3 text-sm text-gray-700">
              Provider: <span className="font-medium">{item.provider}</span>
            </p>
            {item.missing.length > 0 && (
              <p className="mt-2 text-sm text-amber-800">
                Still empty: <span className="font-mono text-xs">{item.missing.join(', ')}</span>
              </p>
            )}
          </li>
        ))}
      </ul>

      <section className="rounded-xl border border-gray-200 bg-white p-5" aria-labelledby="test-heading">
        <h2 id="test-heading" className="text-lg font-semibold text-gray-900">
          Send a test
        </h2>
        <div className="mt-4 grid gap-6 md:grid-cols-2">
          <div className="space-y-3">
            <Input
              id="test-email"
              type="email"
              label="E-mail address"
              helperText="Leave empty to send to your own address"
              value={emailTo}
              onChange={(e) => setEmailTo(e.target.value)}
            />
            <Button isLoading={busy === 'email'} onClick={() => test('email')} disabled={busy !== null}>
              Send test e-mail
            </Button>
          </div>
          <div className="space-y-3">
            <Input
              id="test-sms"
              type="tel"
              label="Phone number"
              helperText="With the country code, or as it is dialled locally"
              value={smsTo}
              onChange={(e) => setSmsTo(e.target.value)}
            />
            <Button isLoading={busy === 'sms'} onClick={() => test('sms')} disabled={busy !== null}>
              Send test text
            </Button>
          </div>
        </div>
      </section>
    </div>
  )
}

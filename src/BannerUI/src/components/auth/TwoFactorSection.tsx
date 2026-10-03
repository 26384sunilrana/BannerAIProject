'use client'

import React, { FormEvent, useCallback, useEffect, useState } from 'react'
import { Button, Input } from '@/components/Common'
import { twoFactorService, TwoFactorSetup, TwoFactorStatus } from '@/api/twoFactorService'
import { getErrorMessage } from '@/api/client'

/**
 * Two-step sign-in with an authenticator app, on the account page. Setting it up: the password again, a picture to scan (or a secret to type),
 * and the first code from the app; then ten one-time recovery codes are shown once, for a lost phone.
 */
export function TwoFactorSection({ forced = false }: { forced?: boolean }) {
  const [status, setStatus] = useState<TwoFactorStatus | null>(null)
  const [loadError, setLoadError] = useState<string | null>(null)
  const [mode, setMode] = useState<'idle' | 'password' | 'scan' | 'codes' | 'disable' | 'renew'>('idle')
  const [setup, setSetup] = useState<TwoFactorSetup | null>(null)
  const [picture, setPicture] = useState<string | null>(null)
  const [password, setPassword] = useState('')
  const [code, setCode] = useState('')
  const [recovery, setRecovery] = useState<string[]>([])
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  const load = useCallback(async () => {
    try {
      setStatus(await twoFactorService.status())
      setLoadError(null)
    } catch (err) {
      setLoadError(getErrorMessage(err, 'Could not load the two-step sign-in settings.'))
    }
  }, [])

  useEffect(() => {
    load()
  }, [load])

  const reset = () => {
    setMode('idle')
    setPassword('')
    setCode('')
    setError(null)
  }

  const run = async (task: () => Promise<void>) => {
    setBusy(true)
    setError(null)
    try {
      await task()
    } catch (err) {
      setError(getErrorMessage(err, 'That did not work.'))
    } finally {
      setBusy(false)
    }
  }

  const begin = (event: FormEvent) => {
    event.preventDefault()
    run(async () => {
      const made = await twoFactorService.beginSetup(password)
      const QRCode = (await import('qrcode')).default
      setPicture(await QRCode.toDataURL(made.uri, { margin: 1, width: 192 }))
      setSetup(made)
      setPassword('')
      setMode('scan')
    })
  }

  const confirm = (event: FormEvent) => {
    event.preventDefault()
    run(async () => {
      setRecovery(await twoFactorService.enable(code))
      setCode('')
      setMode('codes')
      await load()
    })
  }

  const disable = (event: FormEvent) => {
    event.preventDefault()
    run(async () => {
      await twoFactorService.disable(password, code)
      reset()
      await load()
    })
  }

  const renew = (event: FormEvent) => {
    event.preventDefault()
    run(async () => {
      setRecovery(await twoFactorService.newRecoveryCodes(password, code))
      setPassword('')
      setCode('')
      setMode('codes')
      await load()
    })
  }

  return (
    <section aria-labelledby="two-factor-heading" id="two-factor">
      <h2 id="two-factor-heading" className="text-lg font-semibold text-gray-900">
        Two-step sign-in
      </h2>
      <p className="mt-1 max-w-2xl text-sm text-gray-600">
        Besides your password, signing in asks for a six-digit code from an app on your phone (Google Authenticator, Microsoft Authenticator, Authy). A stolen
        password alone then cannot open your account.
      </p>

      {forced && (
        <p role="alert" data-testid="two-factor-required" className="mt-3 max-w-2xl rounded-lg border border-amber-300 bg-amber-50 px-4 py-3 text-sm text-amber-900">
          Administrators must use two-step sign-in. Set it up now: nothing else is open until it is on.
        </p>
      )}
      {loadError && (
        <p role="alert" className="mt-3 text-red-700">
          {loadError}
        </p>
      )}

      {status && mode === 'idle' && (
        <div className="mt-3 space-y-3">
          <p className="text-sm text-gray-800" data-testid="two-factor-status">
            {status.enabled ? `It is on. ${status.recoveryCodesLeft} recovery code${status.recoveryCodesLeft === 1 ? '' : 's'} left.` : 'It is off.'}
          </p>
          <div className="flex flex-wrap gap-2">
            {!status.enabled && <Button onClick={() => setMode('password')}>Set up two-step sign-in</Button>}
            {status.enabled && (
              <Button variant="secondary" onClick={() => setMode('renew')}>
                New recovery codes
              </Button>
            )}
            {status.enabled && !status.required && (
              <Button variant="danger" onClick={() => setMode('disable')}>
                Switch it off
              </Button>
            )}
          </div>
          {status.enabled && status.required && <p className="text-xs text-gray-500">Administrators cannot switch it off.</p>}
        </div>
      )}

      {error && (
        <p role="alert" className="mt-3 text-sm text-red-700">
          {error}
        </p>
      )}

      {mode === 'password' && (
        <form onSubmit={begin} className="mt-3 max-w-sm space-y-3">
          <Input id="tf-password" type="password" label="Your password" value={password} onChange={(e) => setPassword(e.target.value)} autoComplete="current-password" />
          <div className="flex gap-2">
            <Button type="submit" isLoading={busy}>
              Continue
            </Button>
            <Button type="button" variant="secondary" onClick={reset}>
              Cancel
            </Button>
          </div>
        </form>
      )}

      {mode === 'scan' && setup && (
        <form onSubmit={confirm} className="mt-3 max-w-xl space-y-3">
          <ol className="list-decimal space-y-1 pl-5 text-sm text-gray-800">
            <li>Open the authenticator app and add an account (the + button).</li>
            <li>Scan this picture, or choose &ldquo;enter a key&rdquo; and type the secret below.</li>
            <li>Type the six-digit code the app shows now.</li>
          </ol>
          <div className="flex flex-wrap items-center gap-4">
            {picture && (
              // eslint-disable-next-line @next/next/no-img-element
              <img src={picture} alt="QR picture for the authenticator app" width={192} height={192} className="rounded border border-gray-200" />
            )}
            <div>
              <p className="text-xs text-gray-500">Secret</p>
              <p className="font-mono text-lg tracking-wider text-gray-900" data-testid="two-factor-secret">
                {setup.secret}
              </p>
            </div>
          </div>
          <Input id="tf-code" label="Code from the app" inputMode="numeric" autoComplete="one-time-code" value={code} onChange={(e) => setCode(e.target.value)} />
          <div className="flex gap-2">
            <Button type="submit" isLoading={busy}>
              Turn it on
            </Button>
            <Button type="button" variant="secondary" onClick={reset}>
              Cancel
            </Button>
          </div>
        </form>
      )}

      {mode === 'codes' && (
        <div className="mt-3 max-w-xl space-y-3 rounded-xl border border-green-200 bg-green-50 p-4" data-testid="recovery-codes">
          <p className="font-medium text-green-900">Two-step sign-in is on. Keep these recovery codes somewhere safe.</p>
          <p className="text-sm text-green-900">
            Each code works once, if you lose your phone. They are shown only now: write them down or save them in a password manager.
          </p>
          <ul className="grid grid-cols-2 gap-2 font-mono text-base text-gray-900">
            {recovery.map((c) => (
              <li key={c}>{c}</li>
            ))}
          </ul>
          <Button
            onClick={() => {
              setRecovery([])
              reset()
            }}
          >
            I have saved them
          </Button>
        </div>
      )}

      {(mode === 'disable' || mode === 'renew') && (
        <form onSubmit={mode === 'disable' ? disable : renew} className="mt-3 max-w-sm space-y-3">
          <Input id="tf-password" type="password" label="Your password" value={password} onChange={(e) => setPassword(e.target.value)} autoComplete="current-password" />
          <Input id="tf-code" label="Code from the app" inputMode="numeric" autoComplete="one-time-code" value={code} onChange={(e) => setCode(e.target.value)} />
          <div className="flex gap-2">
            <Button type="submit" variant={mode === 'disable' ? 'danger' : 'primary'} isLoading={busy}>
              {mode === 'disable' ? 'Switch it off' : 'Make new recovery codes'}
            </Button>
            <Button type="button" variant="secondary" onClick={reset}>
              Cancel
            </Button>
          </div>
        </form>
      )}
    </section>
  )
}

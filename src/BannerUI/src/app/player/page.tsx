'use client'

import React, { useCallback, useEffect, useRef, useState } from 'react'
import { ScreenPlayer } from '@/components/Display/ScreenPlayer'
import { apiClient } from '@/api/client'
import { saveTokens } from '@/lib/session'
import { deviceApi, forgetScreenSecret, loadScreenSecret, saveScreenSecret } from '@/lib/deviceScreen'

type Phase =
  | { kind: 'starting' }
  | { kind: 'pairing'; code: string; expiresAt: string }
  | { kind: 'playing'; shopId: string; shopName: string }
  | { kind: 'offline' }

const RETRY_MS = 15_000
const POLL_MS = 4_000

const statusOf = (error: unknown) => (error as { response?: { status?: number } })?.response?.status

/**
 * The page to open on a television (or tablet) in the shop: <code>/player</code>. Nobody signs in on it. A new screen shows a six-character code; the
 * owner types that code under <b>Screens</b> in their own login, and from then on the screen plays the shop's banners and ads by itself,
 * and picks up again after a restart. Open <code>/player?reset=1</code> to make it forget the shop and show a new code.
 */
export default function PlayerPage() {
  const [phase, setPhase] = useState<Phase>({ kind: 'starting' })
  const secret = useRef<string | null>(null)
  const timer = useRef<ReturnType<typeof setTimeout>>()
  const alive = useRef(true)

  const enter = useCallback(async (deviceSecret: string) => {
    try {
      const token = await deviceApi.exchange(deviceSecret)
      saveTokens({ accessToken: token.accessToken })
      // an hour-long token is replaced with the screen's own secret, not with a cookie
      apiClient.useDeviceRefresher(async () => {
        try {
          saveTokens({ accessToken: (await deviceApi.exchange(deviceSecret)).accessToken })
          return true
        } catch {
          return false
        }
      })
      if (alive.current) setPhase({ kind: 'playing', shopId: token.shopId, shopName: token.shopName })
    } catch (error) {
      if (!alive.current) return
      if (statusOf(error) === 401) {
        // removed from the shop, or never paired: start again
        forgetScreenSecret()
        secret.current = null
        begin()
      } else {
        setPhase({ kind: 'offline' })
        timer.current = setTimeout(() => enter(deviceSecret), RETRY_MS)
      }
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  const waitForOwner = useCallback(
    (deviceSecret: string, expiresAt: string) => {
      const look = async () => {
        if (!alive.current) return
        try {
          const state = await deviceApi.pollPairing(deviceSecret)
          if (state.status === 'paired') return enter(deviceSecret)
          if (state.status === 'expired' || Date.now() > new Date(expiresAt).getTime()) return begin()
        } catch {
          // no connection: keep showing the code and try again
        }
        timer.current = setTimeout(look, POLL_MS)
      }
      timer.current = setTimeout(look, POLL_MS)
    },
    // eslint-disable-next-line react-hooks/exhaustive-deps
    [enter]
  )

  const begin = useCallback(async () => {
    try {
      const start = await deviceApi.startPairing()
      secret.current = start.deviceSecret
      saveScreenSecret(start.deviceSecret)
      if (!alive.current) return
      setPhase({ kind: 'pairing', code: start.code, expiresAt: start.expiresAt })
      waitForOwner(start.deviceSecret, start.expiresAt)
    } catch {
      if (!alive.current) return
      setPhase({ kind: 'offline' })
      timer.current = setTimeout(begin, RETRY_MS)
    }
  }, [waitForOwner])

  const lost = useCallback(() => {
    forgetScreenSecret()
    secret.current = null
    setPhase({ kind: 'starting' })
    begin()
  }, [begin])

  useEffect(() => {
    alive.current = true
    if (new URLSearchParams(window.location.search).get('reset') === '1') forgetScreenSecret()
    const saved = loadScreenSecret()
    if (saved) {
      secret.current = saved
      // a secret saved while a code was showing is not a pairing yet: the server says so with 401 and a new code is made
      enter(saved)
    } else {
      begin()
    }
    return () => {
      alive.current = false
      clearTimeout(timer.current)
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  if (phase.kind === 'playing') return <ScreenPlayer shopId={phase.shopId} shopName={phase.shopName} onLost={lost} />

  return (
    <main className="flex h-screen w-screen flex-col items-center justify-center bg-slate-900 px-8 text-center text-white" data-testid="player-waiting">
      {phase.kind === 'pairing' ? (
        <>
          <p className="text-2xl text-blue-200">Add this screen to your shop</p>
          <p className="mt-8 font-mono text-8xl font-bold tracking-[0.3em]" data-testid="pairing-code">
            {phase.code.slice(0, 3)} {phase.code.slice(3)}
          </p>
          <ol className="mt-10 max-w-2xl list-decimal space-y-2 text-left text-xl text-blue-100">
            <li>On your phone or computer, sign in to BannerAI as the shop owner.</li>
            <li>Open <b>Screens</b> and choose <b>Add a screen</b>.</li>
            <li>Type the code above. This screen starts by itself.</li>
          </ol>
          <p className="mt-8 text-sm text-blue-300">The code is good for 15 minutes; a new one appears by itself.</p>
        </>
      ) : phase.kind === 'offline' ? (
        <>
          <p className="text-3xl">No connection</p>
          <p className="mt-3 text-lg text-blue-200">Trying again every few seconds…</p>
        </>
      ) : (
        <p className="text-2xl text-blue-200">Starting…</p>
      )}
    </main>
  )
}

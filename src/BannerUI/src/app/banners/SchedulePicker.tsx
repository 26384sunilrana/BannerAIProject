'use client'

import React, { FormEvent, useState } from 'react'
import { Button, Input } from '@/components/Common'
import { checkWindow, formatWindow, fromLocalInput, toLocalInputValue } from '@/lib/dates'
import { bannerListService } from '@/api/workflowService'
import { getErrorMessage } from '@/api/client'
import { BannerSummary } from '@/types/workflow'

interface SchedulePickerProps {
  banner: BannerSummary
  /** True when changing the schedule will send an approved or live banner back for approval. */
  needsReapprovalOnChange: boolean
  onSaved: (message: string) => void | Promise<void>
}

/** Shows when a banner runs and lets the owner change it. The shop shows one banner at a time, so windows cannot overlap. */
export function SchedulePicker({ banner, needsReapprovalOnChange, onSaved }: SchedulePickerProps) {
  const [open, setOpen] = useState(false)
  const [start, setStart] = useState(toLocalInputValue(banner.publishStartAt))
  const [end, setEnd] = useState(toLocalInputValue(banner.publishEndAt))
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  const hasSchedule = !!banner.publishStartAt && !!banner.publishEndAt

  const submit = async (event: FormEvent) => {
    event.preventDefault()
    const check = checkWindow(start, end)
    if (!check.valid) {
      setError(check.error ?? 'Check the dates.')
      return
    }

    setBusy(true)
    setError(null)
    try {
      const result = await bannerListService.setSchedule(banner.id, fromLocalInput(start)!, fromLocalInput(end)!)
      setOpen(false)
      await onSaved(
        result.requiresReapproval
          ? 'Schedule saved. The banner needs to be approved again.'
          : 'Schedule saved.'
      )
    } catch (err) {
      setError(getErrorMessage(err, 'Could not save the schedule.'))
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="w-full">
      <div className="flex flex-wrap items-center gap-3">
        <span className={hasSchedule ? 'text-sm text-gray-700' : 'text-sm text-amber-700'} data-testid="schedule-summary">
          {hasSchedule ? formatWindow(banner.publishStartAt, banner.publishEndAt) : 'Not scheduled'}
        </span>
        <button
          type="button"
          className="text-sm text-blue-600 hover:text-blue-800"
          aria-expanded={open}
          onClick={() => {
            setStart(toLocalInputValue(banner.publishStartAt))
            setEnd(toLocalInputValue(banner.publishEndAt))
            setError(null)
            setOpen(!open)
          }}
        >
          {hasSchedule ? 'Change schedule' : 'Set schedule'}
        </button>
      </div>

      {open && (
        <form onSubmit={submit} noValidate className="mt-3 max-w-xl space-y-3 rounded-lg border border-gray-200 bg-gray-50 p-4">
          {error && (
            <div role="alert" className="rounded-lg border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-800">
              {error}
            </div>
          )}
          <div className="grid gap-3 sm:grid-cols-2">
            <Input id={`start-${banner.id}`} type="datetime-local" label="Starts" value={start} onChange={(e) => setStart(e.target.value)} />
            <Input id={`end-${banner.id}`} type="datetime-local" label="Ends" value={end} onChange={(e) => setEnd(e.target.value)} />
          </div>
          <p className="text-xs text-gray-600">
            Times are in your time zone. Your shop shows one banner at a time, so this cannot overlap another banner,
            even on the same day. Outside any banner's hours your shop shows its own default banner.
          </p>
          {needsReapprovalOnChange && hasSchedule && (
            <p className="text-xs text-amber-800">Changing the schedule sends this banner back for approval.</p>
          )}
          <div className="flex gap-2">
            <Button type="submit" size="sm" isLoading={busy}>
              Save schedule
            </Button>
            <Button type="button" size="sm" variant="secondary" onClick={() => setOpen(false)}>
              Cancel
            </Button>
          </div>
        </form>
      )}
    </div>
  )
}

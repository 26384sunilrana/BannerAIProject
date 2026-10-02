'use client'

import React from 'react'
import { Button } from '@/components/Common'

interface PaginationProps {
  page: number
  pageSize: number
  total: number
  onChange: (page: number) => void
}

/** "Showing 26-50 of 130" with previous / next. Hidden when everything fits on one page. */
export function Pagination({ page, pageSize, total, onChange }: PaginationProps) {
  if (total <= pageSize) {
    return total > 0 ? (
      <p className="text-sm text-gray-600" data-testid="pagination-summary">
        {total} in total
      </p>
    ) : null
  }

  const pages = Math.ceil(total / pageSize)
  const first = (page - 1) * pageSize + 1
  const last = Math.min(page * pageSize, total)

  return (
    <nav className="flex flex-wrap items-center justify-between gap-3" aria-label="Pages">
      <p className="text-sm text-gray-600" data-testid="pagination-summary">
        Showing {first}-{last} of {total}
      </p>
      <div className="flex items-center gap-2">
        <Button size="sm" variant="secondary" disabled={page <= 1} onClick={() => onChange(page - 1)}>
          Previous
        </Button>
        <span className="text-sm text-gray-700">
          Page {page} of {pages}
        </span>
        <Button size="sm" variant="secondary" disabled={page >= pages} onClick={() => onChange(page + 1)}>
          Next
        </Button>
      </div>
    </nav>
  )
}

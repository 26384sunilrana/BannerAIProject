import React from 'react'
import { act, render, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'

jest.mock('@/api/client', () => ({
  apiClient: { get: jest.fn(), post: jest.fn(), put: jest.fn(), delete: jest.fn() },
  API_ORIGIN: 'http://api.test',
  getErrorMessage: (_e: unknown, fallback: string) => fallback,
}))

import { apiClient } from '@/api/client'
import { NotificationBell } from '@/components/layout/NotificationBell'

const api = apiClient as jest.Mocked<typeof apiClient>

const message = (id: string, kind: string, isRead = false, linkUrl: string | null = '/approvals') => ({
  id,
  kind,
  title: `Title ${id}`,
  message: `Text ${id}`,
  linkUrl,
  createdAt: '2030-01-07T10:00:00Z',
  isRead,
})

function serve(items = [message('1', 'WorkflowSubmitted'), message('2', 'WorkflowApproved', true, null)], unread = 1) {
  api.get.mockImplementation(async (path: string) => {
    if (path.startsWith('/notifications/unread-count')) return { unread }
    if (path.startsWith('/notifications?')) return { items, total: items.length, unread, page: 1, pageSize: 10 }
    throw new Error(path)
  })
}

beforeEach(() => {
  jest.clearAllMocks()
  serve()
})

describe('NotificationBell', () => {
  it('shows how many messages are unread', async () => {
    render(<NotificationBell />)

    expect(await screen.findByTestId('bell-count')).toHaveTextContent('1')
    expect(screen.getByRole('button', { name: 'Messages, 1 unread' })).toBeInTheDocument()
  })

  it('shows no badge when everything is read, and caps big numbers', async () => {
    serve([], 0)
    const { unmount } = render(<NotificationBell />)
    await waitFor(() => expect(api.get).toHaveBeenCalled())
    expect(screen.queryByTestId('bell-count')).toBeNull()
    unmount()

    serve([], 250)
    render(<NotificationBell />)
    expect(await screen.findByTestId('bell-count')).toHaveTextContent('99+')
  })

  it('opens the latest messages, with links, and marks one read when clicked', async () => {
    api.post.mockResolvedValue({ unread: 0 })
    render(<NotificationBell />)
    await screen.findByTestId('bell-count')

    await userEvent.click(screen.getByRole('button', { name: /Messages/ }))
    const first = await screen.findByTestId('message-WorkflowSubmitted')
    expect(within(first).getByRole('link')).toHaveAttribute('href', '/approvals')
    expect(within(first).getByLabelText('Unread')).toBeInTheDocument()
    expect(within(screen.getByTestId('message-WorkflowApproved')).queryByLabelText('Unread')).toBeNull()

    await userEvent.click(within(first).getByRole('link'))

    await waitFor(() => expect(api.post).toHaveBeenCalledWith('/notifications/1/read'))
    await waitFor(() => expect(screen.queryByTestId('bell-count')).toBeNull())
  })

  it('marks everything read', async () => {
    api.post.mockResolvedValue({ unread: 0 })
    render(<NotificationBell />)
    await screen.findByTestId('bell-count')
    await userEvent.click(screen.getByRole('button', { name: /Messages/ }))
    await screen.findByTestId('message-WorkflowSubmitted')

    await userEvent.click(screen.getByRole('button', { name: 'Mark all as read' }))

    await waitFor(() => expect(api.post).toHaveBeenCalledWith('/notifications/read-all'))
    expect(screen.queryByTestId('bell-count')).toBeNull()
    expect(screen.queryByLabelText('Unread')).toBeNull()
  })

  it('says so when there are no messages, and when they cannot be loaded', async () => {
    serve([], 0)
    const { unmount } = render(<NotificationBell />)
    await userEvent.click(screen.getByRole('button', { name: 'Messages' }))
    expect(await screen.findByText('No messages yet.')).toBeInTheDocument()
    unmount()

    api.get.mockRejectedValue(new Error('down'))
    render(<NotificationBell />)
    await userEvent.click(screen.getByRole('button', { name: 'Messages' }))
    expect(await screen.findByRole('alert')).toHaveTextContent('could not be loaded')
  })

  it('closes with Escape, and looks again every minute', async () => {
    jest.useFakeTimers({ advanceTimers: true })
    render(<NotificationBell />)
    await screen.findByTestId('bell-count')
    await userEvent.click(screen.getByRole('button', { name: /Messages/ }))
    await screen.findByRole('region', { name: 'Messages' })

    await userEvent.keyboard('{Escape}')
    expect(screen.queryByRole('region', { name: 'Messages' })).toBeNull()

    serve([], 4)
    await act(async () => {
      jest.advanceTimersByTime(60_000)
    })
    await waitFor(() => expect(screen.getByTestId('bell-count')).toHaveTextContent('4'))
    jest.useRealTimers()
  })
})

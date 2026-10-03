import { apiClient } from './client'

export interface AppNotification {
  id: string
  kind: string
  title: string
  message: string
  linkUrl: string | null
  createdAt: string
  isRead: boolean
}

export interface NotificationPage {
  items: AppNotification[]
  total: number
  unread: number
  page: number
  pageSize: number
}

export const notificationService = {
  list(page = 1, pageSize = 10, unreadOnly = false): Promise<NotificationPage> {
    return apiClient.get<NotificationPage>(`/notifications?page=${page}&pageSize=${pageSize}&unreadOnly=${unreadOnly}`)
  },

  async unreadCount(): Promise<number> {
    return (await apiClient.get<{ unread: number }>('/notifications/unread-count')).unread
  },

  markRead(id: string): Promise<{ unread: number }> {
    return apiClient.post<{ unread: number }>(`/notifications/${id}/read`)
  },

  markAllRead(): Promise<{ unread: number }> {
    return apiClient.post<{ unread: number }>('/notifications/read-all')
  },
}

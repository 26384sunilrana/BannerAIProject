import { apiClient } from './client'
import { AdminSubscription, AdminUser, AuditEntry, LifecycleReport, Page } from '@/types/admin'

/** Query string from the values that are set. */
export function toQuery(params: Record<string, string | number | boolean | undefined | null>): string {
  const query = new URLSearchParams()
  for (const [key, value] of Object.entries(params)) {
    if (value === undefined || value === null || value === '' || value === false) continue
    query.set(key, String(value))
  }
  const text = query.toString()
  return text ? `?${text}` : ''
}

export interface UserFilter {
  search?: string
  shopId?: string
  includeInactive?: boolean
  page?: number
  pageSize?: number
}

export interface AuditFilter {
  fromIso?: string
  toIso?: string
  userId?: string
  shopId?: string
  failuresOnly?: boolean
  page?: number
  pageSize?: number
}

export const adminService = {
  listUsers(filter: UserFilter): Promise<Page<AdminUser>> {
    return apiClient.get<Page<AdminUser>>(`/admin/users${toQuery({ ...filter })}`)
  },

  deactivateUser(userId: string): Promise<AdminUser> {
    return apiClient.post<AdminUser>(`/admin/users/${encodeURIComponent(userId)}/deactivate`, {})
  },

  activateUser(userId: string): Promise<AdminUser> {
    return apiClient.post<AdminUser>(`/admin/users/${encodeURIComponent(userId)}/activate`, {})
  },

  unlockUser(userId: string): Promise<AdminUser> {
    return apiClient.post<AdminUser>(`/admin/users/${encodeURIComponent(userId)}/unlock`, {})
  },

  listSubscriptions(filter: { status?: number; search?: string; page?: number; pageSize?: number }): Promise<Page<AdminSubscription>> {
    return apiClient.get<Page<AdminSubscription>>(`/admin/subscriptions${toQuery({ ...filter })}`)
  },

  runLifecycle(): Promise<LifecycleReport> {
    return apiClient.post<LifecycleReport>('/subscriptions/admin/run-lifecycle', {})
  },

  listAuditLogs(filter: AuditFilter): Promise<Page<AuditEntry>> {
    return apiClient.get<Page<AuditEntry>>(
      `/admin/audit-logs${toQuery({
        from: filter.fromIso,
        to: filter.toIso,
        userId: filter.userId,
        shopId: filter.shopId,
        minStatusCode: filter.failuresOnly ? 400 : undefined,
        page: filter.page,
        pageSize: filter.pageSize,
      })}`
    )
  },
}

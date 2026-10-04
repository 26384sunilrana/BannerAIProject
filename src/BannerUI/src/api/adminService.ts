import { apiClient } from './client'
import { AdminSubscription, AdminUser, AuditEntry, LifecycleReport, Page, AdminDeletionRequest } from '@/types/admin'

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

  /** For someone who lost their phone and their recovery codes. */
  async resetTwoFactor(userId: string): Promise<void> {
    await apiClient.post(`/admin/users/${encodeURIComponent(userId)}/reset-two-factor`, {})
  },

  unlockUser(userId: string): Promise<AdminUser> {
    return apiClient.post<AdminUser>(`/admin/users/${encodeURIComponent(userId)}/unlock`, {})
  },

  /** Moves a sales executive to another shop. The shop needs a running subscription and a free login. */
  moveUser(userId: string, shopId: string): Promise<{ toShopId: string }> {
    return apiClient.post<{ toShopId: string }>(`/admin/users/${encodeURIComponent(userId)}/move`, { shopId })
  },

  /** Makes a sales executive of the shop its owner (the previous owner becomes a sales executive). */
  assignOwner(shopId: string, userId: string): Promise<{ newOwnerEmail: string }> {
    return apiClient.post<{ newOwnerEmail: string }>(`/admin/shops/${shopId}/owner`, { userId })
  },

  /** Shops to choose from, by name. */
  async listShopOptions(): Promise<{ id: string; name: string }[]> {
    const page = await apiClient.get<{ items: { id: string; name: string }[] }>('/shops?pageNumber=1&pageSize=100')
    return page.items
  },

  listSubscriptions(filter: { status?: number; search?: string; page?: number; pageSize?: number }): Promise<Page<AdminSubscription>> {
    return apiClient.get<Page<AdminSubscription>>(`/admin/subscriptions${toQuery({ ...filter })}`)
  },

  runLifecycle(): Promise<LifecycleReport> {
    return apiClient.post<LifecycleReport>('/subscriptions/admin/run-lifecycle', {})
  },

  /** The activity log as a CSV file (up to 100,000 rows), with the same filters as the list. */
  exportAuditLogs(filter: AuditFilter): Promise<Blob> {
    return apiClient.get<Blob>(
      `/admin/audit-logs/export${toQuery({
        from: filter.fromIso,
        to: filter.toIso,
        userId: filter.userId,
        shopId: filter.shopId,
        minStatusCode: filter.failuresOnly ? 400 : undefined,
      })}`,
      { responseType: 'blob' }
    )
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

  // Admin Management
  listAllAdmins(): Promise<AdminUser[]> {
    return apiClient.get<AdminUser[]>(`/admin/admins`)
  },

  createAdmin(data: { email: string; password: string; firstName: string; lastName: string }): Promise<AdminUser> {
    return apiClient.post<AdminUser>(`/admin/users/create-admin`, data)
  },

  requestAdminDeletion(adminId: string, reason?: string): Promise<AdminDeletionRequest> {
    return apiClient.post<AdminDeletionRequest>(`/admin/admins/${encodeURIComponent(adminId)}/request-deletion`, { reason })
  },

  getPendingDeletionRequests(): Promise<AdminDeletionRequest[]> {
    return apiClient.get<AdminDeletionRequest[]>(`/admin/super-admin/deletion-requests`)
  },

  approveDeletion(requestId: string): Promise<AdminDeletionRequest> {
    return apiClient.post<AdminDeletionRequest>(`/admin/super-admin/deletion-requests/${encodeURIComponent(requestId)}/approve`, {})
  },

  rejectDeletion(requestId: string): Promise<AdminDeletionRequest> {
    return apiClient.post<AdminDeletionRequest>(`/admin/super-admin/deletion-requests/${encodeURIComponent(requestId)}/reject`, {})
  },
}

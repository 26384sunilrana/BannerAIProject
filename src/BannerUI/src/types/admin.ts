export interface Page<T> {
  items: T[]
  total: number
  page: number
  pageSize: number
}

export interface AdminUser {
  id: string
  email: string
  fullName: string
  shopId: string | null
  roles: string[]
  isActive: boolean
  isLockedOut: boolean
  emailVerified: boolean
  createdAt: string
  lastLoginAttempt: string | null
}

export interface AdminSubscription {
  id: string
  shopId: string
  shopName: string
  planName: string
  status: number
  billingPeriod: number
  currentPrice: number
  renewalDate: string
  autoRenew: boolean
  graceEndsAt: string | null
}

export interface AuditEntry {
  id: string
  occurredAt: string
  userId: string | null
  userEmail: string | null
  shopId: string | null
  method: string
  path: string
  statusCode: number
  ipAddress: string | null
  durationMs: number
}

export interface LifecycleReport {
  planChangesApplied: number
  autoRenewed: number
  renewalsFailed: number
  enteredGrace: number
  expired: number
  messagesSent: number
}

export interface AdminDeletionRequest {
  id: string
  adminIdToDelete: string
  adminToDeleteEmail: string
  adminToDeleteName: string
  requestedByAdminId: string
  requestedByAdminEmail: string
  status: number
  statusName: string
  approvedByAdminId: string | null
  approvedByAdminEmail: string | null
  reason: string | null
  createdAt: string
  approvedAt: string | null
}

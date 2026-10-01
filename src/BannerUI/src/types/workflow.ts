export type WorkflowStatus =
  | 'Draft'
  | 'PendingApproval'
  | 'Approved'
  | 'Published'
  | 'Rejected'
  | 'Unpublished'
  | 'Archived'

export interface WorkflowEvent {
  eventType: string
  actorName: string
  occurredAt: string
  comment?: string | null
  outcome?: string | null
}

export interface PublishWorkflow {
  id: string
  bannerId: string
  shopId: string
  submittedByUserId: string
  status: WorkflowStatus
  submittedAt: string
  approvedAt?: string | null
  publishedAt?: string | null
  rejectedAt?: string | null
  rejectionReason?: string | null
  events: WorkflowEvent[]
  createdAt: string
  updatedAt: string
}

export interface BannerSummary {
  id: string
  shopId: string
  name: string
  description: string
  width: number
  height: number
  isPublished: boolean
  componentCount: number
  /** When the banner is shown (UTC); null until a schedule is set. */
  publishStartAt?: string | null
  publishEndAt?: string | null
  createdAt: string
  updatedAt: string
}

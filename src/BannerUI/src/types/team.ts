export interface TeamMember {
  userId: string
  email: string
  fullName: string
  isApprover: boolean
}

export interface ShopTeam {
  shopId: string
  ownerUserId: string | null
  ownerIsApprover: boolean
  maxSalesExecutives: number
  salesExecutives: TeamMember[]
}

export interface AddSalesExecutiveRequest {
  email: string
  password: string
  firstName?: string
  lastName?: string
}

export interface MyApprovalRole {
  shopId: string
  isOwner: boolean
  canApprove: boolean
}

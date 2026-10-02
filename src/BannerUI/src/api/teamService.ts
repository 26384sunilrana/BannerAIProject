import { apiClient } from './client'
import { AddSalesExecutiveRequest, MyApprovalRole, ShopTeam, TeamMember } from '@/types/team'

export const teamService = {
  getTeam(shopId: string): Promise<ShopTeam> {
    return apiClient.get<ShopTeam>(`/shops/${shopId}/team`)
  },

  getMyRole(shopId: string): Promise<MyApprovalRole> {
    return apiClient.get<MyApprovalRole>(`/shops/${shopId}/team/my-role`)
  },

  addSalesExecutive(shopId: string, request: AddSalesExecutiveRequest): Promise<TeamMember> {
    return apiClient.post<TeamMember>(`/shops/${shopId}/team/executives`, request)
  },

  async removeSalesExecutive(shopId: string, userId: string): Promise<void> {
    await apiClient.delete(`/shops/${shopId}/team/executives/${encodeURIComponent(userId)}`)
  },

  setApprovers(shopId: string, ownerIsApprover: boolean, approverUserIds: string[]): Promise<ShopTeam> {
    return apiClient.put<ShopTeam>(`/shops/${shopId}/team/approvers`, { ownerIsApprover, approverUserIds })
  },

  /** Hands the shop to one of the sales executives. Needs the owner's password. Everyone involved is signed out. */
  transferOwnership(shopId: string, newOwnerUserId: string, password: string): Promise<{ newOwnerEmail: string }> {
    return apiClient.post<{ newOwnerEmail: string }>(`/shops/${shopId}/team/transfer-ownership`, { newOwnerUserId, password })
  },
}

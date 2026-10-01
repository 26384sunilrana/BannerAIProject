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
}

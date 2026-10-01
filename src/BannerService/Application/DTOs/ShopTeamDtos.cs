namespace BannerService.Application.DTOs
{
    public class AddSalesExecutiveDto
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
    }

    public class SetApproversDto
    {
        /// <summary>When true the shop owner approves banners.</summary>
        public bool OwnerIsApprover { get; set; } = true;

        /// <summary>User ids of sales executives who also (or instead) approve banners.</summary>
        public List<string>? ApproverUserIds { get; set; }
    }

    public class TeamMemberDto
    {
        public string UserId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public bool IsApprover { get; set; }
    }

    public class ShopTeamDto
    {
        public Guid ShopId { get; set; }
        public Guid? OwnerUserId { get; set; }
        public bool OwnerIsApprover { get; set; }
        public int MaxSalesExecutives { get; set; }
        public List<TeamMemberDto> SalesExecutives { get; set; } = new();
    }
}

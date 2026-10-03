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

        /// <summary>User ids of sales executives who approve the ads other executives book. Null leaves the list as it is.</summary>
        public List<string>? AdApproverUserIds { get; set; }
    }

    public class MyApprovalRoleDto
    {
        public Guid ShopId { get; set; }
        public bool IsOwner { get; set; }
        public bool CanApprove { get; set; }
        public bool CanApproveAds { get; set; }
    }

    public class TeamMemberDto
    {
        public string UserId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public bool IsApprover { get; set; }
        public bool IsAdApprover { get; set; }
    }

    public class ShopTeamDto
    {
        public Guid ShopId { get; set; }
        public Guid? OwnerUserId { get; set; }
        public bool OwnerIsApprover { get; set; }

        /// <summary>The shop has a new owner who has not yet confirmed who approves; nothing can be approved until then.</summary>
        public bool ApprovalReviewRequired { get; set; }
        public int MaxSalesExecutives { get; set; }
        public List<TeamMemberDto> SalesExecutives { get; set; } = new();
    }
}

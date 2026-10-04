namespace BannerService.Domain.Entities
{
    public class Role
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

        // Predefined roles
        public const string SuperAdmin = "SuperAdmin";
        public const string Admin = "Admin";
        public const string ShopOwner = "ShopOwner";
        public const string SalesExecutive = "SalesExecutive";

        public static readonly List<Role> DefaultRoles = new()
        {
            new Role { Id = "0", Name = SuperAdmin, Description = "Permanent product owner with absolute control" },
            new Role { Id = "1", Name = Admin, Description = "Global administrator with full access" },
            new Role { Id = "2", Name = ShopOwner, Description = "Shop owner with shop management access" },
            new Role { Id = "3", Name = SalesExecutive, Description = "Sales executive with limited access" }
        };
    }
}

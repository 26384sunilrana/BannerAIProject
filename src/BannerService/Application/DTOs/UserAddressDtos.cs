namespace BannerService.Application.DTOs
{
    public class CreateUserAddressDto
    {
        public string? AddressLine { get; set; }
        public string? City { get; set; }
        public string? CountryCode { get; set; }
        public int? StateId { get; set; }
        public int? DistrictId { get; set; }
        public string? PostalCode { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string? Label { get; set; }
        public bool IsPrimary { get; set; } = false;
    }

    public class UpdateUserAddressDto
    {
        public string? AddressLine { get; set; }
        public string? City { get; set; }
        public string? CountryCode { get; set; }
        public int? StateId { get; set; }
        public int? DistrictId { get; set; }
        public string? PostalCode { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string? Label { get; set; }
    }

    public class UserAddressDto
    {
        public Guid Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string? AddressLine { get; set; }
        public string? City { get; set; }
        public string? CountryCode { get; set; }
        public string? CountryName { get; set; }
        public int? StateId { get; set; }
        public string? StateName { get; set; }
        public int? DistrictId { get; set; }
        public string? DistrictName { get; set; }
        public string? PostalCode { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string? Label { get; set; }
        public bool IsPrimary { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}

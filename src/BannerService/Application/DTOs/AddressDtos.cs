namespace BannerService.Application.DTOs
{
    public class CountryDto
    {
        public string ISOCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? RegionName { get; set; }
        public string? PhoneCode { get; set; }
        public bool IsActive { get; set; }
    }

    public class StateDto
    {
        public int Id { get; set; }
        public string CountryCode { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? RegionType { get; set; }
        public bool IsActive { get; set; }
    }

    public class DistrictDto
    {
        public int Id { get; set; }
        public int StateId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? RegionType { get; set; }
        public bool IsActive { get; set; }
    }

    public class AddressLookupDto
    {
        public string? CountryCode { get; set; }
        public int? StateId { get; set; }
        public int? DistrictId { get; set; }
    }

    public class ShopAddressDto
    {
        public string? CountryCode { get; set; }
        public int? StateId { get; set; }
        public int? DistrictId { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? PostalCode { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
    }

    public class AddressValidationResultDto
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
        public AddressValidationResultDto() { }
        public AddressValidationResultDto(bool isValid, params string[] errors)
        {
            IsValid = isValid;
            Errors = errors.ToList();
        }
    }
}

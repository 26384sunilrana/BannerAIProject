namespace BannerService.Application.Validators
{
    using BannerService.Domain.Entities;
    using System.Text.RegularExpressions;

    public class ShopValidator
    {
        private const int MaxNameLength = 256;
        private const int MinNameLength = 2;
        private const int MaxDescriptionLength = 2000;
        private const int MaxHierarchyDepth = 10;
        private const string PhoneNumberPattern = @"^\+?[1-9]\d{1,14}$";
        private const string WebsitePattern = @"^(https?://)?([a-zA-Z0-9-]+\.)+[a-zA-Z]{2,}";
        private const string PostalCodePattern = @"^[A-Z0-9\s-]{3,20}$";

        public static (bool isValid, string? errorMessage) ValidateCreateShop(
            string name,
            string? description,
            string? phoneNumber,
            string? website,
            double? latitude,
            double? longitude)
        {
            if (string.IsNullOrWhiteSpace(name))
                return (false, "Shop name is required");

            if (name.Length < MinNameLength || name.Length > MaxNameLength)
                return (false, $"Shop name must be between {MinNameLength} and {MaxNameLength} characters");

            if (!string.IsNullOrWhiteSpace(description) && description.Length > MaxDescriptionLength)
                return (false, $"Description cannot exceed {MaxDescriptionLength} characters");

            if (!string.IsNullOrWhiteSpace(phoneNumber) && !IsValidPhoneNumber(phoneNumber))
                return (false, "Invalid phone number format");

            if (!string.IsNullOrWhiteSpace(website) && !IsValidWebsite(website))
                return (false, "Invalid website URL");

            if (!IsValidCoordinates(latitude, longitude))
                return (false, "Invalid coordinates. Both latitude and longitude must be provided together");

            return (true, null);
        }

        public static (bool isValid, string? errorMessage) ValidateHierarchy(
            Guid shopId,
            Guid? parentShopId,
            Guid? currentParentShopId)
        {
            if (parentShopId == shopId)
                return (false, "A shop cannot be its own parent");

            if (parentShopId == currentParentShopId)
                return (true, null);

            return (true, null);
        }

        public static bool IsValidPhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return true;

            return Regex.IsMatch(phoneNumber, PhoneNumberPattern);
        }

        public static bool IsValidWebsite(string website)
        {
            if (string.IsNullOrWhiteSpace(website))
                return true;

            try
            {
                var uri = new Uri(website);
                return uri.Scheme is "http" or "https";
            }
            catch
            {
                return false;
            }
        }

        public static bool IsValidCoordinates(double? latitude, double? longitude)
        {
            if (latitude.HasValue && !longitude.HasValue || !latitude.HasValue && longitude.HasValue)
                return false;

            if (!latitude.HasValue || !longitude.HasValue)
                return true;

            if (latitude < -90 || latitude > 90)
                return false;

            if (longitude < -180 || longitude > 180)
                return false;

            return true;
        }

        public static bool IsValidPostalCode(string? postalCode)
        {
            if (string.IsNullOrWhiteSpace(postalCode))
                return true;

            return Regex.IsMatch(postalCode, PostalCodePattern, RegexOptions.IgnoreCase);
        }

        public static (bool isValid, string? errorMessage) ValidateStatusTransition(
            ShopStatus currentStatus,
            ShopStatus newStatus)
        {
            // Allow any status transition
            // Validation for child shops should happen at service level
            return (true, null);
        }
    }
}

namespace BannerService.Application.Validators
{
    public static class UserAddressValidator
    {
        private const int MaxAddressLineLength = 256;
        private const int MaxCityLength = 128;
        private const int MaxPostalCodeLength = 20;
        private const int MaxLabelLength = 50;

        public static (bool isValid, string[] errors) ValidateCreate(
            string? addressLine,
            string? city,
            string? postalCode,
            string? label,
            double? latitude,
            double? longitude)
        {
            var errors = new List<string>();

            if (!string.IsNullOrEmpty(addressLine) && addressLine.Length > MaxAddressLineLength)
                errors.Add($"Address line cannot exceed {MaxAddressLineLength} characters");

            if (!string.IsNullOrEmpty(city) && city.Length > MaxCityLength)
                errors.Add($"City cannot exceed {MaxCityLength} characters");

            if (!string.IsNullOrEmpty(postalCode))
            {
                if (postalCode.Length > MaxPostalCodeLength)
                    errors.Add($"Postal code cannot exceed {MaxPostalCodeLength} characters");

                if (!IsValidPostalCode(postalCode))
                    errors.Add("Postal code format is invalid. Use alphanumeric characters, spaces, and hyphens only");
            }

            if (!string.IsNullOrEmpty(label) && label.Length > MaxLabelLength)
                errors.Add($"Label cannot exceed {MaxLabelLength} characters");

            if (!IsValidCoordinates(latitude, longitude))
                errors.Add("Latitude and longitude must both be provided or both be null. Latitude must be between -90 and 90, longitude between -180 and 180");

            return (errors.Count == 0, errors.ToArray());
        }

        public static (bool isValid, string[] errors) ValidateUpdate(
            string? addressLine,
            string? city,
            string? postalCode,
            string? label,
            double? latitude,
            double? longitude)
        {
            return ValidateCreate(addressLine, city, postalCode, label, latitude, longitude);
        }

        private static bool IsValidCoordinates(double? latitude, double? longitude)
        {
            // Both must be provided or both must be null
            if ((latitude == null && longitude != null) || (latitude != null && longitude == null))
                return false;

            // If both are null, it's valid
            if (latitude == null && longitude == null)
                return true;

            // Both are provided, validate ranges
            return latitude >= -90 && latitude <= 90 && longitude >= -180 && longitude <= 180;
        }

        private static bool IsValidPostalCode(string postalCode)
        {
            // Allow alphanumeric, spaces, and hyphens
            return postalCode.All(c => char.IsLetterOrDigit(c) || c == ' ' || c == '-');
        }
    }
}

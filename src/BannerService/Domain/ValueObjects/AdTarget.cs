namespace BannerService.Domain.ValueObjects
{
    public class AdTarget
    {
        public List<string> AudienceSegments { get; set; } = new();
        public List<string> DemographicFilters { get; set; } = new();
        public List<string> TargetLocations { get; set; } = new();
        public List<string> ExcludedLocations { get; set; } = new();
        public List<string> DeviceTypes { get; set; } = new(); // Desktop, Mobile, Tablet
        public List<string> OperatingSystems { get; set; } = new(); // Windows, iOS, Android, etc.
        public bool TargetAllUsers { get; set; } = false;
        public int? MinimumAge { get; set; }
        public int? MaximumAge { get; set; }
        public List<string> InterestCategories { get; set; } = new();
        public Dictionary<string, string> CustomAttributes { get; set; } = new();

        public bool IsValid()
        {
            if (TargetAllUsers)
                return true;

            return AudienceSegments.Any() ||
                   DemographicFilters.Any() ||
                   TargetLocations.Any() ||
                   DeviceTypes.Any() ||
                   InterestCategories.Any();
        }
    }
}

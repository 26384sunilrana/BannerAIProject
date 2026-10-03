namespace BannerService.Application.Services
{
    using Domain.Entities;
    using Domain.Interfaces;
    using Domain.Services;
    using System.Text.Json.Serialization;

    public class AdRateInput
    {
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public AdRateLevel Level { get; set; }
        public string? CountryCode { get; set; }
        public int? StateId { get; set; }
        public int? CityId { get; set; }

        /// <summary>Null for every kind of ad.</summary>
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ShopAdKind? Kind { get; set; }
        public decimal PricePerHour { get; set; }
        public int ShopSharePercent { get; set; } = 100;
    }

    public class AdRateDto
    {
        public Guid Id { get; set; }
        public string Level { get; set; } = string.Empty;
        public string? CountryCode { get; set; }
        public int? StateId { get; set; }
        public int? CityId { get; set; }

        /// <summary>A readable name of the place, for example "Mumbai, Maharashtra".</summary>
        public string Place { get; set; } = string.Empty;
        public string? Kind { get; set; }
        public decimal PricePerHour { get; set; }
        public int ShopSharePercent { get; set; }
        public bool IsActive { get; set; }
    }

    /// <summary>The administrator's price list for ads, by place and kind of ad.</summary>
    public class AdRateService
    {
        private readonly IAdRateRepository _rates;
        private readonly ILocationRepository _locations;

        public AdRateService(IAdRateRepository rates, ILocationRepository locations)
        {
            _rates = rates;
            _locations = locations;
        }

        public async Task<List<AdRateDto>> ListAsync()
        {
            var result = new List<AdRateDto>();
            foreach (var rate in await _rates.ListAsync(activeOnly: false))
                result.Add(await ToDtoAsync(rate));
            return result.OrderByDescending(r => r.IsActive).ThenBy(r => r.Place).ThenBy(r => r.Kind).ToList();
        }

        public async Task<AdRateDto> CreateAsync(AdRateInput input)
        {
            var rate = new AdRate();
            await ApplyAsync(rate, input);
            await _rates.AddAsync(rate);
            return await ToDtoAsync(rate);
        }

        public async Task<AdRateDto> UpdateAsync(Guid id, AdRateInput input)
        {
            var rate = await _rates.GetByIdAsync(id) ?? throw new KeyNotFoundException("Rate not found.");
            await ApplyAsync(rate, input);
            rate.IsActive = true;
            await _rates.SaveAsync();
            return await ToDtoAsync(rate);
        }

        /// <summary>A rate is switched off, not removed: ads already booked keep the price they were booked at.</summary>
        public async Task<AdRateDto> DeactivateAsync(Guid id)
        {
            var rate = await _rates.GetByIdAsync(id) ?? throw new KeyNotFoundException("Rate not found.");
            rate.IsActive = false;
            rate.UpdatedAt = DateTime.UtcNow;
            await _rates.SaveAsync();
            return await ToDtoAsync(rate);
        }

        /// <summary>The rate that applies to an ad on this shop, or null.</summary>
        public async Task<AdRate?> FindForAsync(Shop shop, ShopAdKind kind) =>
            AdRateRules.Pick(await _rates.FindCandidatesAsync(shop.CountryCode, shop.StateId, shop.CityId), shop.CountryCode, shop.StateId, shop.CityId, kind);

        private async Task ApplyAsync(AdRate rate, AdRateInput input)
        {
            rate.Level = input.Level;
            rate.CountryCode = input.Level == AdRateLevel.Country ? input.CountryCode?.Trim().ToUpperInvariant() : null;
            rate.StateId = input.Level == AdRateLevel.State ? input.StateId : null;
            rate.CityId = input.Level == AdRateLevel.City ? input.CityId : null;
            rate.Kind = input.Kind;
            rate.PricePerHour = input.PricePerHour;
            rate.ShopSharePercent = input.ShopSharePercent;
            rate.UpdatedAt = DateTime.UtcNow;

            if (AdRateRules.Validate(rate) is { } problem) throw new ArgumentException(problem);

            // the place has to exist
            if (rate.Level == AdRateLevel.Country && await _locations.FindCountryAsync(rate.CountryCode!) == null) throw new ArgumentException("That country does not exist.");
            if (rate.Level == AdRateLevel.State && await _locations.FindStateAsync(rate.StateId!.Value) == null) throw new ArgumentException("That state does not exist.");
            if (rate.Level == AdRateLevel.City && await _locations.FindCityAsync(rate.CityId!.Value) == null) throw new ArgumentException("That city does not exist.");

            var same = await _rates.FindSamePlaceAsync(rate.Level, rate.CountryCode, rate.StateId, rate.CityId, rate.Kind, rate.Id);
            if (same != null) throw new InvalidOperationException("There is already a rate for this place and kind of ad. Change that one instead.");
        }

        private async Task<AdRateDto> ToDtoAsync(AdRate rate)
        {
            string place = rate.Level switch
            {
                AdRateLevel.All => "Every shop",
                AdRateLevel.Country => (await _locations.FindCountryAsync(rate.CountryCode!))?.Name ?? rate.CountryCode ?? "?",
                AdRateLevel.State => (await _locations.FindStateAsync(rate.StateId!.Value)) is { } s ? $"{s.Name}" : "?",
                AdRateLevel.City => (await _locations.FindCityAsync(rate.CityId!.Value)) is { } c ? $"{c.Name}" : "?",
                _ => "?",
            };
            return new AdRateDto
            {
                Id = rate.Id,
                Level = rate.Level.ToString(),
                CountryCode = rate.CountryCode,
                StateId = rate.StateId,
                CityId = rate.CityId,
                Place = place,
                Kind = rate.Kind?.ToString(),
                PricePerHour = rate.PricePerHour,
                ShopSharePercent = rate.ShopSharePercent,
                IsActive = rate.IsActive,
            };
        }
    }
}

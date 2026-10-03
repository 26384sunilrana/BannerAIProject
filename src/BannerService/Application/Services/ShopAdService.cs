namespace BannerService.Application.Services
{
    using Domain.Entities;
    using Domain.Interfaces;
    using Domain.Services;
    using Domain.ValueObjects;
    using Infrastructure.Security;
    using System.Text.Json.Serialization;

    /// <summary>The person doing something with ads. A shop id is set for owners and executives; an admin has none.</summary>
    public record AdActor(Guid UserId, string Name, ShopAdSource Source, Guid? ShopId);

    public class DailyHoursInput
    {
        public int StartMinutes { get; set; }
        public int EndMinutes { get; set; }
        public int Days { get; set; } = DailySchedule.AllDays;
    }

    public class ShopAdInput
    {
        public string AdvertiserName { get; set; } = string.Empty;
        public string Headline { get; set; } = string.Empty;
        public string? Body { get; set; }
        public Guid? MediaFileId { get; set; }
        public string Background { get; set; } = "#ffffff";
        public string TextColor { get; set; } = "#111111";
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ShopAdKind Kind { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ShopAdPlacement Placement { get; set; }
        public int SpacePercent { get; set; }
        public int PopupSeconds { get; set; }
        public int PopupEveryMinutes { get; set; }
        public DateTime StartAt { get; set; }
        public DateTime EndAt { get; set; }
        public DailyHoursInput? Daily { get; set; }
    }

    public class ShopAdDto
    {
        public Guid Id { get; set; }
        public Guid ShopId { get; set; }
        public string ShopName { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public string AdvertiserName { get; set; } = string.Empty;
        public string Headline { get; set; } = string.Empty;
        public string? Body { get; set; }
        public Guid? MediaFileId { get; set; }
        public string? MediaUrl { get; set; }
        public string Background { get; set; } = string.Empty;
        public string TextColor { get; set; } = string.Empty;
        public string Kind { get; set; } = string.Empty;
        public string Placement { get; set; } = string.Empty;
        public int SpacePercent { get; set; }
        public int PopupSeconds { get; set; }
        public int PopupEveryMinutes { get; set; }
        public DateTime StartAt { get; set; }
        public DateTime EndAt { get; set; }
        public int? DailyStartMinutes { get; set; }
        public int? DailyEndMinutes { get; set; }
        public int ActiveDays { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? DecidedByName { get; set; }
        public DateTime? DecidedAt { get; set; }
        public string? DecisionNote { get; set; }
        public Guid CreatedByUserId { get; set; }

        /// <summary>Administrator ads: what an hour is worth, and the part of it the shop is paid.</summary>
        public decimal? PricePerHour { get; set; }
        public int? ShopSharePercent { get; set; }
        public DateTime? StoppedAt { get; set; }

        /// <summary>Why an administrator has to review the ad (health wording), or null.</summary>
        public string? ComplianceNote { get; set; }

        /// <summary>What the caller may do with it right now: edit, submit, approve, reject, cancel, override.</summary>
        public List<string> Can { get; set; } = new();
    }

    /// <summary>An ad booked on every shop in a place at once.</summary>
    public class CampaignInput : ShopAdInput
    {
        public string? CountryCode { get; set; }
        public int? StateId { get; set; }
        public int? CityId { get; set; }
    }

    public class CampaignSkippedDto
    {
        public Guid ShopId { get; set; }
        public string ShopName { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
    }

    public class CampaignResultDto
    {
        public int Shops { get; set; }
        public int Booked { get; set; }
        public List<CampaignSkippedDto> Skipped { get; set; } = new();
    }

    public class ShopAdEventDto
    {
        public string Action { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string? Note { get; set; }
        public DateTime At { get; set; }
    }

    /// <summary>
    /// Booking ads on shops' screens. The admin, a shop owner and a shop's sales executives may book; an owner's or admin's ad is approved
    /// as soon as it is submitted, an executive's waits for the owner. Everything that happens to an ad is written to its history.
    /// </summary>
    public class ShopAdService
    {
        private const int LinkMinutes = 240;

        /// <summary>The most shops one campaign may reach; a larger place has to be split.</summary>
        public const int MaxCampaignShops = 500;

        private readonly IShopAdRepository _ads;
        private readonly IShopRepository _shops;
        private readonly IMediaFileRepository _media;
        private readonly IMediaUrlSigner _signer;
        private readonly NotificationService _notifications;
        private readonly AdRateService _rates;
        private readonly ILocationRepository _locations;

        public ShopAdService(IShopAdRepository ads, IShopRepository shops, IMediaFileRepository media, IMediaUrlSigner signer, NotificationService notifications, AdRateService rates, ILocationRepository locations)
        {
            _locations = locations;
            _rates = rates;
            _ads = ads;
            _shops = shops;
            _media = media;
            _signer = signer;
            _notifications = notifications;
        }

        // ----- booking

        public async Task<ShopAdDto> CreateAsync(AdActor actor, Guid? requestedShopId, ShopAdInput input)
        {
            var shopId = ResolveShop(actor, requestedShopId);
            var shop = await _shops.GetByIdAsync(shopId) ?? throw new KeyNotFoundException("Shop not found.");

            var ad = new ShopAd { ShopId = shopId, CreatedByUserId = actor.UserId, Source = actor.Source };
            await ApplyAsync(actor, ad, input);

            await _ads.AddAsync(ad, Event(ad, actor, "Created"));
            return await ToDtoAsync(actor, ad, shop);
        }

        public async Task<ShopAdDto> UpdateAsync(AdActor actor, Guid adId, ShopAdInput input)
        {
            var ad = await LoadAsync(actor, adId);
            EnsureCanManage(actor, ad);
            if (!ad.CanBeEdited)
                throw new InvalidOperationException("Only an ad that has not been sent in, or was sent back, can be changed. Cancel it and book a new one instead.");

            await ApplyAsync(actor, ad, input);
            ad.Status = ShopAdStatus.Draft;
            ad.DecidedAt = null;
            ad.DecidedByName = null;
            ad.DecidedByUserId = null;
            ad.DecisionNote = null;
            ad.UpdatedAt = DateTime.UtcNow;
            await _ads.SaveAsync(ad, Event(ad, actor, "Edited"));
            return await ToDtoAsync(actor, ad);
        }

        public async Task<ShopAdDto> SubmitAsync(AdActor actor, Guid adId, DateTime? nowUtc = null)
        {
            var now = nowUtc ?? DateTime.UtcNow;
            var ad = await LoadAsync(actor, adId);
            EnsureCanManage(actor, ad);
            if (ad.Status is not (ShopAdStatus.Draft or ShopAdStatus.Rejected))
                throw new InvalidOperationException("This ad has already been sent in.");

            if (ShopAdRules.Validate(ad) is { } problem) throw new ArgumentException(problem);
            if (ad.EndAt <= now) throw new ArgumentException("The ad would already be over. Choose later dates.");
            await EnsureSlotFreeAsync(ad);

            var shop = await _shops.GetByIdAsync(ad.ShopId);

            // an administrator ad is priced by the rate for the shop's place, fixed now
            if (ad.Source == ShopAdSource.Admin)
            {
                var rate = shop == null ? null : await _rates.FindForAsync(shop, ad.Kind);
                if (rate == null)
                    throw new InvalidOperationException("No rate is set for this shop's place and this kind of ad. Set one under Ad rates first.");
                ad.PricePerHour = AdRateRules.HourlyPrice(rate, ad);
                ad.ShopSharePercent = rate.ShopSharePercent;
            }

            var auto = ad.Source is ShopAdSource.Admin or ShopAdSource.ShopOwner;
            var review = ad.ComplianceNote != null && auto;
            ad.Status = review ? ShopAdStatus.PendingCompliance : auto ? ShopAdStatus.Approved : ShopAdStatus.PendingApproval;
            ad.UpdatedAt = now;
            if (review)
            {
                await _ads.SaveAsync(ad, Event(ad, actor, "Submitted", "Waiting for compliance review: " + ad.ComplianceNote));
                await TellAdminsToReviewAsync(ad, shop);
                return await ToDtoAsync(actor, ad, shop);
            }
            if (auto)
            {
                ad.DecidedByUserId = actor.UserId;
                ad.DecidedByName = actor.Name;
                ad.DecidedAt = now;
                ad.DecisionNote = "Approved automatically: booked by " + (ad.Source == ShopAdSource.Admin ? "the administrator" : "the shop owner");
            }
            await _ads.SaveAsync(ad, Event(ad, actor, auto ? "Approved" : "Submitted", auto ? ad.DecisionNote : null));

            if (ad.Source == ShopAdSource.SalesExecutive)
                await _notifications.NotifyShopOwnersAsync(ad.ShopId, "AdSubmitted", "Ad waiting for approval",
                    $"{actor.Name} booked \"{ad.Headline}\" for {ad.AdvertiserName}.", "/ads");
            else if (ad.Source == ShopAdSource.Admin)
                await _notifications.NotifyShopOwnersAsync(ad.ShopId, "AdBookedByAdmin", ad.Kind == ShopAdKind.Mega ? "A major ad was booked on your screen" : "An ad was booked on your screen",
                    $"\"{ad.Headline}\" for {ad.AdvertiserName} runs on your screen from {ad.StartAt:d MMM yyyy} to {ad.EndAt:d MMM yyyy}.", "/ads");

            return await ToDtoAsync(actor, ad, shop);
        }

        public async Task<ShopAdDto> ApproveAsync(AdActor actor, Guid adId, string? note, DateTime? nowUtc = null)
        {
            var now = nowUtc ?? DateTime.UtcNow;
            var ad = await LoadAsync(actor, adId);
            if (ad.Status == ShopAdStatus.PendingCompliance) EnsureCanReview(actor);
            else EnsureCanDecide(actor, ad);
            if (ad.Status is not (ShopAdStatus.PendingApproval or ShopAdStatus.PendingCompliance)) throw new InvalidOperationException("This ad is not waiting for approval.");
            if (actor.Source == ShopAdSource.ShopOwner && (await _shops.GetByIdAsync(ad.ShopId))?.ApprovalReviewRequired == true)
                throw new InvalidOperationException("This shop has a new owner. Open Team and confirm who approves before anything is approved.");
            if (ad.EndAt <= now) throw new InvalidOperationException("The ad would already be over. Send it back so the dates can be changed.");

            await EnsureSlotFreeAsync(ad);

            // approved by the owner, but the wording still has to be reviewed by an administrator
            if (ad.Status == ShopAdStatus.PendingApproval && ad.ComplianceNote != null)
            {
                ad.Status = ShopAdStatus.PendingCompliance;
                ad.UpdatedAt = now;
                await _ads.SaveAsync(ad, Event(ad, actor, "Approved", (string.IsNullOrWhiteSpace(note) ? string.Empty : note.Trim() + ". ") + "Waiting for compliance review: " + ad.ComplianceNote));
                await TellAdminsToReviewAsync(ad, await _shops.GetByIdAsync(ad.ShopId));
                return await ToDtoAsync(actor, ad);
            }

            var reviewed = ad.Status == ShopAdStatus.PendingCompliance;
            Decide(ad, actor, ShopAdStatus.Approved, note, now);
            if (reviewed)
            {
                ad.ComplianceApprovedAt = now;
                ad.ComplianceApprovedBy = actor.Name;
            }
            await _ads.SaveAsync(ad, Event(ad, actor, reviewed ? "Compliance approved" : "Approved", note));
            await _notifications.NotifyUsersAsync(new[] { ad.CreatedByUserId.ToString() }, "AdApproved", "Ad approved", $"\"{ad.Headline}\" was approved by {actor.Name}.", "/ads");
            if (reviewed && ad.Source == ShopAdSource.Admin)
                await _notifications.NotifyShopOwnersAsync(ad.ShopId, "AdBookedByAdmin", ad.Kind == ShopAdKind.Mega ? "A major ad was booked on your screen" : "An ad was booked on your screen",
                    $"\"{ad.Headline}\" for {ad.AdvertiserName} runs on your screen from {ad.StartAt:d MMM yyyy} to {ad.EndAt:d MMM yyyy}.", "/ads");
            return await ToDtoAsync(actor, ad);
        }

        public async Task<ShopAdDto> RejectAsync(AdActor actor, Guid adId, string? reason, DateTime? nowUtc = null)
        {
            var ad = await LoadAsync(actor, adId);
            if (ad.Status == ShopAdStatus.PendingCompliance) EnsureCanReview(actor);
            else EnsureCanDecide(actor, ad);
            if (ad.Status is not (ShopAdStatus.PendingApproval or ShopAdStatus.PendingCompliance)) throw new InvalidOperationException("This ad is not waiting for approval.");
            if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("Say why the ad is sent back.");

            reason = reason.Trim();
            if (reason.Length > 500) reason = reason[..500];
            var wasReview = ad.Status == ShopAdStatus.PendingCompliance;
            Decide(ad, actor, ShopAdStatus.Rejected, reason, nowUtc ?? DateTime.UtcNow);
            await _ads.SaveAsync(ad, Event(ad, actor, wasReview ? "Compliance rejected" : "Rejected", reason));
            await _notifications.NotifyUsersAsync(new[] { ad.CreatedByUserId.ToString() }, "AdRejected", "Ad sent back", $"\"{ad.Headline}\" was sent back: {reason}", "/ads");
            return await ToDtoAsync(actor, ad);
        }

        public async Task<ShopAdDto> CancelAsync(AdActor actor, Guid adId)
        {
            var ad = await LoadAsync(actor, adId);
            if (ad.Status == ShopAdStatus.Cancelled) throw new InvalidOperationException("This ad is already cancelled.");
            if (ad.Status == ShopAdStatus.Overridden) throw new InvalidOperationException("This ad was stopped by the shop and is already over.");
            if (actor.Source != ShopAdSource.Admin && ad.Source == ShopAdSource.Admin)
                throw new UnauthorizedAccessException("This ad was booked by the administrator. Ask the administrator to cancel it, or override it if you have a better offer.");
            EnsureCanManage(actor, ad);

            var now = DateTime.UtcNow;
            if (ad.Status == ShopAdStatus.Approved) ad.StoppedAt = now; // it ran until now
            ad.Status = ShopAdStatus.Cancelled;
            ad.UpdatedAt = now;
            await _ads.SaveAsync(ad, Event(ad, actor, "Cancelled"));
            return await ToDtoAsync(actor, ad);
        }

        /// <summary>
        /// A shop owner stops an approved administrator ad because a better local offer came. The slot is free at once, the ad is paid for the
        /// hours it ran, and the administrators are told inside the application.
        /// </summary>
        public async Task<ShopAdDto> OverrideAsync(AdActor actor, Guid adId, string? note, DateTime? nowUtc = null)
        {
            var now = nowUtc ?? DateTime.UtcNow;
            var ad = await LoadAsync(actor, adId);
            if (actor.Source != ShopAdSource.ShopOwner) throw new UnauthorizedAccessException("Only the shop owner can override an administrator ad.");
            if (ad.Source != ShopAdSource.Admin) throw new InvalidOperationException("Only an ad booked by the administrator needs to be overridden. You can cancel your own ads.");
            if (ad.Status != ShopAdStatus.Approved) throw new InvalidOperationException("Only an ad that is booked can be overridden.");
            if (ad.EndAt <= now) throw new InvalidOperationException("This ad is already over.");

            note = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
            if (note is { Length: > 500 }) note = note[..500];

            ad.Status = ShopAdStatus.Overridden;
            ad.StoppedAt = now;
            ad.UpdatedAt = now;
            await _ads.SaveAsync(ad, Event(ad, actor, "Overridden", note));

            var shop = await _shops.GetByIdAsync(ad.ShopId);
            await _notifications.NotifyAdminsAsync("AdOverridden", "An ad was overridden by the shop",
                $"{actor.Name} stopped \"{ad.Headline}\" ({ad.AdvertiserName}) on {shop?.Name ?? "a shop"}." + (note == null ? string.Empty : $" Reason: {note}"), "/ads");
            return await ToDtoAsync(actor, ad, shop);
        }

        /// <summary>
        /// The administrator books one ad on every active shop in a place (a city, a state, a country or everywhere). Each shop is booked on its own
        /// and priced by its own rate; a shop where the ad cannot go (the screen is taken, no rate) is skipped with the reason, and the rest are
        /// booked. Every shop owner is told in the application.
        /// </summary>
        public async Task<CampaignResultDto> CampaignAsync(AdActor actor, CampaignInput input, DateTime? nowUtc = null)
        {
            if (actor.Source != ShopAdSource.Admin) throw new UnauthorizedAccessException("Only the administrator can book an ad on a whole place.");
            if (input.MediaFileId != null) throw new ArgumentException("Ads booked by the administrator are text only for now.");

            var shops = await _locations.ActiveShopsInPlaceAsync(input.CountryCode, input.StateId, input.CityId);
            if (shops.Count == 0) throw new ArgumentException("There is no active shop in that place.");
            if (shops.Count > MaxCampaignShops)
                throw new ArgumentException($"That place has more than {MaxCampaignShops} shops. Choose a smaller place, such as a city.");

            // check the shape of the ad once before touching any shop
            var probe = new ShopAd { ShopId = shops[0].Id, Source = ShopAdSource.Admin };
            await ApplyAsync(actor, probe, input);

            var result = new CampaignResultDto { Shops = shops.Count };
            foreach (var shop in shops)
            {
                Guid? draftId = null;
                try
                {
                    var created = await CreateAsync(actor, shop.Id, input);
                    draftId = created.Id;
                    await SubmitAsync(actor, created.Id, nowUtc);
                    result.Booked++;
                }
                catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or KeyNotFoundException)
                {
                    // do not leave a stray draft behind on the shop
                    if (draftId != null) await CancelAsync(actor, draftId.Value);
                    result.Skipped.Add(new CampaignSkippedDto { ShopId = shop.Id, ShopName = shop.Name, Reason = ex.Message });
                }
            }
            return result;
        }

        // ----- looking

        public async Task<List<ShopAdDto>> ListAsync(AdActor actor, Guid? requestedShopId, DateTime? from, DateTime? to)
        {
            var shopId = actor.Source == ShopAdSource.Admin ? requestedShopId : actor.ShopId;
            if (actor.Source != ShopAdSource.Admin && requestedShopId != null && requestedShopId != actor.ShopId)
                throw new UnauthorizedAccessException("You can only see the ads of your own shop.");

            var ads = await _ads.ListAsync(shopId, from, to, holdingSlotOnly: false);
            if (actor.Source == ShopAdSource.SalesExecutive)
                ads = ads.Where(a => a.HoldsSlot || a.CreatedByUserId == actor.UserId).ToList();

            var names = new Dictionary<Guid, Shop?>();
            var result = new List<ShopAdDto>();
            foreach (var ad in ads)
            {
                if (!names.ContainsKey(ad.ShopId)) names[ad.ShopId] = await _shops.GetByIdAsync(ad.ShopId);
                result.Add(await ToDtoAsync(actor, ad, names[ad.ShopId]));
            }
            return result;
        }

        public async Task<List<ShopAdEventDto>> HistoryAsync(AdActor actor, Guid adId)
        {
            var ad = await LoadAsync(actor, adId);
            return (await _ads.GetEventsAsync(ad.Id))
                .Select(e => new ShopAdEventDto { Action = e.Action, UserName = e.UserName, Note = e.Note, At = DateTime.SpecifyKind(e.At, DateTimeKind.Utc) })
                .ToList();
        }

        /// <summary>The ads on a shop's screen at this moment, for the screen to draw.</summary>
        public async Task<List<ShopAdDto>> LiveAsync(Guid shopId, DateTime? nowUtc = null)
        {
            var now = nowUtc ?? DateTime.UtcNow;
            var shop = await _shops.GetByIdAsync(shopId) ?? throw new KeyNotFoundException("Shop not found.");
            var zone = ShopTimeZone.Resolve(shop).Zone;

            var candidates = (await _ads.FindSlotHoldersAsync(shopId, now, now.AddSeconds(1)))
                .Where(a => a.Status == ShopAdStatus.Approved && ScheduleOccurrences.IsLive(a.GetWindow(), a.GetDailySchedule(), zone, now));

            var result = new List<ShopAdDto>();
            foreach (var ad in candidates)
                result.Add(await ToDtoAsync(null, ad, shop));
            return result;
        }

        // ----- helpers

        private static Guid ResolveShop(AdActor actor, Guid? requested)
        {
            if (actor.Source == ShopAdSource.Admin)
                return requested ?? throw new ArgumentException("Choose the shop the ad is for.");
            if (actor.ShopId == null) throw new UnauthorizedAccessException("Your account is not linked to a shop.");
            if (requested != null && requested != actor.ShopId) throw new UnauthorizedAccessException("You can only book ads on your own shop.");
            return actor.ShopId.Value;
        }

        private async Task<ShopAd> LoadAsync(AdActor actor, Guid adId)
        {
            var ad = await _ads.GetByIdAsync(adId);
            // another shop's ad is reported as missing rather than forbidden
            if (ad == null || (actor.Source != ShopAdSource.Admin && ad.ShopId != actor.ShopId))
                throw new KeyNotFoundException("Ad not found.");
            return ad;
        }

        /// <summary>Admin: any ad. Owner: ads of the shop. Executive: only the ads they booked themselves.</summary>
        private static void EnsureCanManage(AdActor actor, ShopAd ad)
        {
            var allowed = actor.Source switch
            {
                ShopAdSource.Admin => true,
                ShopAdSource.ShopOwner => ad.Source != ShopAdSource.Admin,
                _ => ad.CreatedByUserId == actor.UserId,
            };
            if (!allowed) throw new UnauthorizedAccessException("You cannot change this ad.");
        }

        /// <summary>The owner decides on an executive's ad; the admin may decide on any.</summary>
        private static void EnsureCanDecide(AdActor actor, ShopAd ad)
        {
            if (actor.Source == ShopAdSource.SalesExecutive) throw new UnauthorizedAccessException("Only the shop owner can approve ads.");
            if (ad.Source != ShopAdSource.SalesExecutive && actor.Source != ShopAdSource.Admin)
                throw new UnauthorizedAccessException("This ad does not need your approval.");
        }

        private static void EnsureCanReview(AdActor actor)
        {
            if (actor.Source != ShopAdSource.Admin) throw new UnauthorizedAccessException("Only the administrator reviews ads that mention health or personal information.");
        }

        private async Task TellAdminsToReviewAsync(ShopAd ad, Shop? shop) =>
            await _notifications.NotifyAdminsAsync("AdNeedsReview", "Ad needs a compliance review",
                $"\"{ad.Headline}\" for {ad.AdvertiserName} on {shop?.Name ?? "a shop"} uses health wording and waits for your review.", "/ads");

        private static void Decide(ShopAd ad, AdActor actor, ShopAdStatus status, string? note, DateTime now)
        {
            ad.Status = status;
            ad.DecidedByUserId = actor.UserId;
            ad.DecidedByName = actor.Name;
            ad.DecidedAt = now;
            ad.DecisionNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
            ad.UpdatedAt = now;
        }

        private async Task ApplyAsync(AdActor actor, ShopAd ad, ShopAdInput input)
        {
            ad.AdvertiserName = input.AdvertiserName ?? string.Empty;
            ad.Headline = input.Headline ?? string.Empty;
            ad.Body = input.Body;
            ad.Background = input.Background ?? string.Empty;
            ad.TextColor = input.TextColor ?? string.Empty;
            ad.Kind = input.Kind;
            ad.Placement = input.Placement;
            ad.SpacePercent = input.SpacePercent;
            ad.PopupSeconds = input.PopupSeconds;
            ad.PopupEveryMinutes = input.PopupEveryMinutes;
            ad.StartAt = DateTime.SpecifyKind(input.StartAt, DateTimeKind.Utc);
            ad.EndAt = DateTime.SpecifyKind(input.EndAt, DateTimeKind.Utc);

            var daily = input.Daily == null ? null : new DailySchedule(input.Daily.StartMinutes, input.Daily.EndMinutes, input.Daily.Days);
            ad.SetDaily(daily);

            if (input.MediaFileId is { } mediaId)
            {
                if (actor.Source == ShopAdSource.Admin)
                    throw new ArgumentException("Ads booked by the administrator are text only for now.");
                var file = await _media.GetByIdAsync(mediaId, ad.ShopId);
                if (file is not { Status: (int)MediaFileStatus.Active })
                    throw new ArgumentException("The picture must be one of the shop's uploaded files.");
                if (file.FileType != (int)MediaFileType.Image)
                    throw new ArgumentException("The picture must be an image, not a video.");
            }
            ad.MediaFileId = input.MediaFileId;

            // trim and tidy before checking, so a headline of spaces is not accepted
            ShopAdRules.Normalise(ad);
            if (ShopAdRules.Validate(ad) is { } problem) throw new ArgumentException(problem);

            // ads carry no personal or health information: contact and identity details are refused, health wording goes to an administrator
            var found = AdContentScreen.Screen(("advertiser name", ad.AdvertiserName), ("headline", ad.Headline), ("text", ad.Body));
            if (found.Blocks.Count > 0)
                throw new ArgumentException(string.Join(" ", found.Blocks) + " Ads must not carry personal details such as phone numbers, e-mail addresses, identity or card numbers, or health information about people.");
            ad.ComplianceNote = found.Reviews.Count == 0 ? null : string.Join(" ", found.Reviews);
            ad.ComplianceApprovedAt = null;
            ad.ComplianceApprovedBy = null;
        }

        private async Task EnsureSlotFreeAsync(ShopAd ad)
        {
            var shop = await _shops.GetByIdAsync(ad.ShopId) ?? throw new KeyNotFoundException("Shop not found.");
            var others = await _ads.FindSlotHoldersAsync(ad.ShopId, ad.StartAt, ad.EndAt);
            if (ShopAdRules.FindConflict(ad, others, ShopTimeZone.Resolve(shop).Zone) is { } conflict)
                throw new InvalidOperationException(conflict);
        }

        private static ShopAdEvent Event(ShopAd ad, AdActor actor, string action, string? note = null) => new()
        {
            ShopAdId = ad.Id,
            ShopId = ad.ShopId,
            UserId = actor.UserId,
            UserName = actor.Name,
            Action = action,
            Note = note,
        };

        private async Task<ShopAdDto> ToDtoAsync(AdActor? actor, ShopAd ad, Shop? shop = null)
        {
            shop ??= await _shops.GetByIdAsync(ad.ShopId);

            string? url = null;
            if (ad.MediaFileId is { } mediaId)
            {
                var file = await _media.GetByIdAsync(mediaId, ad.ShopId);
                if (file is { Status: (int)MediaFileStatus.Active })
                {
                    var expires = DateTime.UtcNow.AddMinutes(LinkMinutes);
                    url = $"/api/media/{mediaId}/download?expires={new DateTimeOffset(expires).ToUnixTimeSeconds()}&sig={_signer.Sign(mediaId, expires)}";
                }
            }

            return new ShopAdDto
            {
                Id = ad.Id,
                ShopId = ad.ShopId,
                ShopName = shop?.Name ?? string.Empty,
                Source = ad.Source.ToString(),
                AdvertiserName = ad.AdvertiserName,
                Headline = ad.Headline,
                Body = ad.Body,
                MediaFileId = ad.MediaFileId,
                MediaUrl = url,
                Background = ad.Background,
                TextColor = ad.TextColor,
                Kind = ad.Kind.ToString(),
                Placement = ad.Placement.ToString(),
                SpacePercent = ad.SpacePercent,
                PopupSeconds = ad.PopupSeconds,
                PopupEveryMinutes = ad.PopupEveryMinutes,
                StartAt = DateTime.SpecifyKind(ad.StartAt, DateTimeKind.Utc),
                EndAt = DateTime.SpecifyKind(ad.EndAt, DateTimeKind.Utc),
                DailyStartMinutes = ad.DailyStartMinutes,
                DailyEndMinutes = ad.DailyEndMinutes,
                ActiveDays = ad.ActiveDays,
                Status = ad.Status.ToString(),
                DecidedByName = ad.DecidedByName,
                DecidedAt = ad.DecidedAt == null ? null : DateTime.SpecifyKind(ad.DecidedAt.Value, DateTimeKind.Utc),
                DecisionNote = ad.DecisionNote,
                CreatedByUserId = ad.CreatedByUserId,
                PricePerHour = ad.PricePerHour,
                ShopSharePercent = ad.ShopSharePercent,
                ComplianceNote = ad.ComplianceNote,
                StoppedAt = ad.StoppedAt == null ? null : DateTime.SpecifyKind(ad.StoppedAt.Value, DateTimeKind.Utc),
                Can = actor == null ? new List<string>() : Abilities(actor, ad),
            };
        }

        private static List<string> Abilities(AdActor actor, ShopAd ad)
        {
            var can = new List<string>();
            bool manage;
            try { EnsureCanManage(actor, ad); manage = true; } catch (UnauthorizedAccessException) { manage = false; }

            if (manage && ad.CanBeEdited) { can.Add("edit"); can.Add("submit"); }
            if (manage && ad.Status is not (ShopAdStatus.Cancelled or ShopAdStatus.Overridden)) can.Add("cancel");
            if (actor.Source == ShopAdSource.ShopOwner && ad.Source == ShopAdSource.Admin && ad.Status == ShopAdStatus.Approved && ad.EndAt > DateTime.UtcNow)
                can.Add("override");
            if (ad.Status == ShopAdStatus.PendingApproval)
            {
                try { EnsureCanDecide(actor, ad); can.Add("approve"); can.Add("reject"); } catch (UnauthorizedAccessException) { }
            }
            else if (ad.Status == ShopAdStatus.PendingCompliance && actor.Source == ShopAdSource.Admin)
            {
                can.Add("approve");
                can.Add("reject");
            }
            return can;
        }
    }
}

namespace BannerService.Domain.Services
{
    using System.Text.Json;

    /// <summary>
    /// Checks the optional settings a component keeps in its properties: its visual effect and, for pictures, the list of
    /// pictures that rotate. They are stored with the rest of the component, so they are versioned, previewed and
    /// approved with the banner. Values the player cannot handle are refused here rather than found on the shop screen.
    /// </summary>
    public static class ComponentSettingsValidator
    {
        public static readonly string[] EffectKinds = { "none", "fadeIn", "slideLeft", "slideRight", "slideUp", "zoomIn", "pulse", "float" };
        public static readonly string[] SlideTransitions = { "fade", "slideLeft", "slideRight", "zoom" };

        public const int MinEffectMs = 100, MaxEffectMs = 10_000, MaxEffectDelayMs = 30_000;
        public const int MaxSlides = 20;
        public const int MinSlideSeconds = 1, MaxSlideSeconds = 60;
        public const int MinTransitionMs = 200, MaxTransitionMs = 2_000;

        /// <summary>Validates the "effect" setting of any component. Absent is fine.</summary>
        public static void ValidateEffect(IDictionary<string, object>? properties)
        {
            var effect = Find(properties, "effect");
            if (effect is not { } value || value.ValueKind == JsonValueKind.Null)
                return;
            if (value.ValueKind != JsonValueKind.Object)
                throw new ArgumentException("effect must be an object with kind, durationMs and delayMs");

            var kind = ReadString(value, "kind");
            if (kind == null || !EffectKinds.Contains(kind))
                throw new ArgumentException($"effect.kind must be one of: {string.Join(", ", EffectKinds)}");

            var duration = ReadInt(value, "durationMs");
            if (duration is < MinEffectMs or > MaxEffectMs)
                throw new ArgumentException($"effect.durationMs must be {MinEffectMs}-{MaxEffectMs}");

            var delay = ReadInt(value, "delayMs");
            if (delay is < 0 or > MaxEffectDelayMs)
                throw new ArgumentException($"effect.delayMs must be 0-{MaxEffectDelayMs}");
        }

        /// <summary>Validates the rotating pictures of an image component: "slides" and how they rotate. Absent is fine.</summary>
        public static void ValidateImageSlides(IDictionary<string, object>? properties)
        {
            var slides = Find(properties, "slides");
            if (slides is { } list && list.ValueKind != JsonValueKind.Null)
            {
                if (list.ValueKind != JsonValueKind.Array)
                    throw new ArgumentException("slides must be a list");
                if (list.GetArrayLength() > MaxSlides)
                    throw new ArgumentException($"A picture can rotate through at most {MaxSlides} pictures");

                foreach (var slide in list.EnumerateArray())
                {
                    if (slide.ValueKind != JsonValueKind.Object)
                        throw new ArgumentException("Every slide needs a mediaFileId");

                    var id = ReadString(slide, "mediaFileId");
                    if (string.IsNullOrWhiteSpace(id) || id.Length > 64)
                        throw new ArgumentException("Every slide needs a mediaFileId");

                    var alt = ReadString(slide, "alt");
                    if (alt is { Length: > 200 })
                        throw new ArgumentException("A slide's alt text can have up to 200 characters");
                }
            }

            var seconds = Find(properties, "slideIntervalSeconds");
            if (seconds is { ValueKind: not JsonValueKind.Null } s && (s.ValueKind != JsonValueKind.Number || !s.TryGetDouble(out var secondsValue) || secondsValue < MinSlideSeconds || secondsValue > MaxSlideSeconds))
                throw new ArgumentException($"slideIntervalSeconds must be {MinSlideSeconds}-{MaxSlideSeconds}");

            var transition = Find(properties, "slideTransition");
            if (transition is { ValueKind: not JsonValueKind.Null } t && (t.ValueKind != JsonValueKind.String || !SlideTransitions.Contains(t.GetString())))
                throw new ArgumentException($"slideTransition must be one of: {string.Join(", ", SlideTransitions)}");

            var transitionMs = Find(properties, "slideTransitionMs");
            if (transitionMs is { ValueKind: not JsonValueKind.Null } ms && (ms.ValueKind != JsonValueKind.Number || !ms.TryGetInt32(out var msValue) || msValue < MinTransitionMs || msValue > MaxTransitionMs))
                throw new ArgumentException($"slideTransitionMs must be {MinTransitionMs}-{MaxTransitionMs}");
        }

        // ----- reading loosely typed properties

        private static JsonElement? Find(IDictionary<string, object>? properties, string name)
        {
            if (properties == null)
                return null;

            foreach (var pair in properties)
            {
                if (!string.Equals(pair.Key, name, StringComparison.OrdinalIgnoreCase))
                    continue;

                return pair.Value is JsonElement element
                    ? element
                    : JsonSerializer.SerializeToElement(pair.Value);
            }

            return null;
        }

        private static string? ReadString(JsonElement element, string name)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
                    return property.Value.ValueKind == JsonValueKind.String ? property.Value.GetString() : null;
            }
            return null;
        }

        /// <summary>The whole number under the name, or null when it is missing or not a whole number (which fails the range checks).</summary>
        private static int? ReadInt(JsonElement element, string name)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
                    return property.Value.ValueKind == JsonValueKind.Number && property.Value.TryGetInt32(out var number) ? number : -1;
            }
            return -1;
        }
    }
}

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using BepInEx.Configuration;
using TimeConfig.Models;

namespace TimeConfig.Configuration;

public sealed class ActiveSettings
{
    public const float PreserveFloat = -1f;
    public const int PreserveInt = -1;
    public const long PreserveLong = -1L;

    private readonly ConfigEntry<bool> _enableCustomTiming;
    private readonly ConfigEntry<float> _dayDurationSeconds;
    private readonly ConfigEntry<int> _daysBeforeQuota;
    private readonly ConfigEntry<long> _startingQuota;
    private readonly ConfigEntry<float> _catchUpFactor;
    private readonly ConfigEntry<string> _quotaMultipliersCsv;
    private readonly ConfigEntry<string> _activeProfileName;
    private readonly ProfileStore _profileStore;

    private ActiveSettings(
        ConfigEntry<bool> enableCustomTiming,
        ConfigEntry<float> dayDurationSeconds,
        ConfigEntry<int> daysBeforeQuota,
        ConfigEntry<long> startingQuota,
        ConfigEntry<float> catchUpFactor,
        ConfigEntry<string> quotaMultipliersCsv,
        ConfigEntry<string> activeProfileName,
        ProfileStore profileStore)
    {
        _enableCustomTiming = enableCustomTiming;
        _dayDurationSeconds = dayDurationSeconds;
        _daysBeforeQuota = daysBeforeQuota;
        _startingQuota = startingQuota;
        _catchUpFactor = catchUpFactor;
        _quotaMultipliersCsv = quotaMultipliersCsv;
        _activeProfileName = activeProfileName;
        _profileStore = profileStore;
    }

    public bool IsEnabled => _enableCustomTiming.Value;

    /// <summary>
    /// The profile store used to save and load named timing profiles.
    /// Access this from other components to persist or enumerate profiles.
    /// </summary>
    public ProfileStore Profiles => _profileStore;

    public static ActiveSettings Bind(ConfigFile config)
    {
        var enableCustomTiming = config.Bind(
            "General",
            "EnableCustomTiming",
            false,
            "Enable host-authoritative timing overrides for new sessions.");

        var dayDurationSeconds = config.Bind(
            "Timing",
            "DayDurationSeconds",
            PreserveFloat,
            "Override the vanilla day duration in seconds. Use -1 to preserve the loaded value.");

        var daysBeforeQuota = config.Bind(
            "Timing",
            "DaysBeforeQuota",
            PreserveInt,
            "Override the number of days before the quota is due. Use -1 to preserve the loaded value.");

        var startingQuota = config.Bind(
            "Quota",
            "StartingQuota",
            PreserveLong,
            "Override the starting quota for new sessions. Use -1 to preserve the loaded value.");

        var catchUpFactor = config.Bind(
            "Quota",
            "CatchUpFactor",
            PreserveFloat,
            "Override the quota catch-up factor. Use -1 to preserve the loaded value.");

        var quotaMultipliersCsv = config.Bind(
            "Quota",
            "QuotaMultipliersCsv",
            string.Empty,
            "Comma-separated quota multipliers. Leave blank to preserve the loaded values.");

        var activeProfileName = config.Bind(
            "Profiles",
            "ActiveProfileName",
            string.Empty,
            "Name of a saved profile to load on startup. Overrides all individual Timing/Quota entries " +
            "when non-empty. Use the ProfileStore API or edit profile files under " +
            "BepInEx/config/TimeConfig/profiles/.");

        var configDir = Path.GetDirectoryName(config.ConfigFilePath)
            ?? System.AppDomain.CurrentDomain.BaseDirectory;
        var profileStore = new ProfileStore(configDir);

        return new ActiveSettings(
            enableCustomTiming,
            dayDurationSeconds,
            daysBeforeQuota,
            startingQuota,
            catchUpFactor,
            quotaMultipliersCsv,
            activeProfileName,
            profileStore);
    }

    public bool TryCreateResolvedProfile(
        GameSettings baseSettings,
        out TimingProfile profile,
        out IReadOnlyList<ValidationOutcome> outcomes)
    {
        var validation = new List<ValidationOutcome>();

        // Named profile takes precedence over individual config entries.
        StoredProfile? stored = null;
        var profileName = _activeProfileName.Value?.Trim() ?? string.Empty;
        if (!string.IsNullOrEmpty(profileName) && _profileStore.TryLoad(profileName, out StoredProfile? loaded))
            stored = loaded;

        var resolvedDayDuration = ResolveFloat(
            stored?.DayDurationSeconds ?? _dayDurationSeconds.Value,
            baseSettings.dayDuration);

        var resolvedDaysBeforeQuota = ResolveInt(
            stored?.DaysBeforeQuota ?? _daysBeforeQuota.Value,
            baseSettings.daysBeforeQuota);

        var resolvedStartingQuota = ResolveLong(
            stored?.StartingQuota ?? _startingQuota.Value,
            baseSettings.startingQuota);

        var resolvedCatchUpFactor = ResolveFloat(
            stored?.CatchUpFactor ?? _catchUpFactor.Value,
            baseSettings.catchUpFactor);

        var resolvedQuotaMultipliers = baseSettings.quotas ?? new float[0];
        if (stored?.QuotaMultipliers is { Length: > 0 } storedMultipliers)
        {
            resolvedQuotaMultipliers = storedMultipliers;
        }
        else
        {
            var rawMultipliers = _quotaMultipliersCsv.Value?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(rawMultipliers))
            {
                if (TimingProfileValidator.TryParseQuotaMultipliers(rawMultipliers, out var parsed, out var parseError))
                    resolvedQuotaMultipliers = parsed;
                else if (parseError is not null)
                    validation.Add(parseError);
            }
        }

        var resolvedName = !string.IsNullOrEmpty(profileName) ? profileName : "ActiveConfig";
        profile = new TimingProfile(
            IsEnabled ? resolvedName : "Vanilla",
            !IsEnabled,
            resolvedDayDuration,
            resolvedDaysBeforeQuota,
            resolvedStartingQuota,
            resolvedCatchUpFactor,
            resolvedQuotaMultipliers.ToArray());

        validation.AddRange(TimingProfileValidator.Validate(profile));
        outcomes = validation;

        return validation.All(outcome => outcome.Status != ValidationStatus.Error);
    }

    private static float ResolveFloat(float value, float vanilla) =>
        value > PreserveFloat ? value : vanilla;

    private static int ResolveInt(int value, int vanilla) =>
        value > PreserveInt ? value : vanilla;

    private static long ResolveLong(long value, long vanilla) =>
        value > PreserveLong ? value : vanilla;
}

using System;
using System.Linq;
using BepInEx.Logging;
using TimeConfig.Configuration;
using TimeConfig.Models;
using UnityEngine;

namespace TimeConfig.Runtime;

public static class TimingCoordinator
{
    private static ActiveSettings? _settings;
    private static ManualLogSource? _log;

    public static SessionTimingState? CurrentState { get; private set; }

    public static void Initialize(ActiveSettings settings, ManualLogSource log)
    {
        _settings = settings;
        _log = log;
    }

    public static bool TryApplyFromResources(string context)
    {
        EnsureInitialized();

        var gameSettings = Resources.Load<GameSettings>("GameSettings");
        if (gameSettings == null)
        {
            _log!.LogDebug($"No GameSettings resource was available during {context}.");
            return false;
        }

        return TryApplyToGameSettings(gameSettings, context);
    }

    public static bool TryApplyToGameSettings(GameSettings gameSettings, string context)
    {
        EnsureInitialized();

        if (!_settings!.IsEnabled)
        {
            CurrentState = BuildState(
                "Vanilla",
                true,
                gameSettings.dayDuration,
                gameSettings.daysBeforeQuota,
                gameSettings.startingQuota,
                gameSettings.catchUpFactor,
                gameSettings.quotas?.Length ?? 0,
                context);

            return false;
        }

        if (!_settings.TryCreateResolvedProfile(gameSettings, out var profile, out var outcomes))
        {
            foreach (var outcome in outcomes.Where(o => o.Status == ValidationStatus.Error))
            {
                _log!.LogError($"[{context}] {outcome.TargetField}: {outcome.Message}");
            }

            return false;
        }

        gameSettings.dayDuration = profile.DayDurationSeconds;
        gameSettings.daysBeforeQuota = profile.DaysBeforeQuota;
        gameSettings.startingQuota = profile.StartingQuota;
        gameSettings.catchUpFactor = profile.CatchUpFactor;
        gameSettings.quotas = profile.QuotaMultipliers.ToArray();

        CurrentState = BuildState(
            profile.Name,
            profile.IsVanillaProfile,
            profile.DayDurationSeconds,
            profile.DaysBeforeQuota,
            profile.StartingQuota,
            profile.CatchUpFactor,
            profile.QuotaMultipliers.Count,
            context);

        _log!.LogInfo(
            $"[{context}] Applied timing profile '{profile.Name}' " +
            $"(dayDuration={profile.DayDurationSeconds}, daysBeforeQuota={profile.DaysBeforeQuota}, " +
            $"startingQuota={profile.StartingQuota}, catchUpFactor={profile.CatchUpFactor}, " +
            $"quotaMultipliers={profile.QuotaMultipliers.Count}).");

        return true;
    }

    private static SessionTimingState BuildState(
        string profileName,
        bool isVanilla,
        float dayDurationSeconds,
        int daysBeforeQuota,
        long startingQuota,
        float catchUpFactor,
        int quotaMultiplierCount,
        string context)
    {
        return new SessionTimingState(
            context,
            profileName,
            isVanilla,
            dayDurationSeconds,
            daysBeforeQuota,
            startingQuota,
            catchUpFactor,
            quotaMultiplierCount,
            DateTimeOffset.UtcNow);
    }

    private static void EnsureInitialized()
    {
        if (_settings == null || _log == null)
        {
            throw new InvalidOperationException("TimingCoordinator.Initialize must run before timing overrides are applied.");
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using TimeConfig.Configuration;
using TimeConfig.Models;
using UnityEngine;

namespace TimeConfig.Runtime;

public static class TimingCoordinator
{
    private static readonly AccessTools.FieldRef<GameManager, GameSettings> GameSettingsRef =
        AccessTools.FieldRefAccess<GameManager, GameSettings>("_gs");

    private static readonly AccessTools.FieldRef<SaveManager, SaveData> CurrentSaveDataRef =
        AccessTools.FieldRefAccess<SaveManager, SaveData>("currentSaveData");

    private static readonly System.Reflection.PropertyInfo? HasDayStartedProperty =
        AccessTools.Property(typeof(GameManager), nameof(GameManager.HasDayStarted));

    private static readonly System.Reflection.PropertyInfo? NetworkTimerProperty =
        AccessTools.Property(typeof(GameManager), "Network_timer");

    private static readonly System.Reflection.PropertyInfo? NetworkCurrentQuotaProperty =
        AccessTools.Property(typeof(GameManager), "NetworkcurrentQuota");

    private static readonly System.Reflection.PropertyInfo? NetworkRequiredQuotaProperty =
        AccessTools.Property(typeof(GameManager), "NetworkrequiredQuotaToNextFloor");

    private static readonly System.Reflection.PropertyInfo? NetworkDaysLeftProperty =
        AccessTools.Property(typeof(GameManager), "NetworkdaysLeft");

    private static readonly System.Reflection.PropertyInfo? NetworkDaysPassedProperty =
        AccessTools.Property(typeof(GameManager), "NetworkdaysPassed");

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

        if (!TryGetActiveGameSettings(context, out var gameSettings, includeSceneInstance: false))
        {
            return false;
        }

        return TryApplyToGameSettings(gameSettings!, context);
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

        if (!TryResolveProfile(gameSettings, context, out var resolvedProfile))
        {
            return false;
        }

        var profile = resolvedProfile!;

        ApplyResolvedProfileToGameSettings(gameSettings, profile, context);
        return true;
    }

    public static bool TryApplyInitialQuotaToSaveData(SaveData saveData, string context)
    {
        EnsureInitialized();

        if (saveData == null || !_settings!.IsEnabled)
        {
            return false;
        }

        if (!TryGetActiveGameSettings(context, out var gameSettings))
        {
            return false;
        }

        if (!TryResolveProfile(gameSettings!, context, out var resolvedProfile))
        {
            return false;
        }

        ApplyInitialQuotaToSaveData(saveData, resolvedProfile!, context);

        return true;
    }

    public static bool TryGetResolvedProfile(string context, out TimingProfile? profile)
    {
        EnsureInitialized();

        if (!TryGetActiveGameSettings(context, out var gameSettings))
        {
            profile = null;
            return false;
        }

        if (!_settings!.IsEnabled)
        {
            profile = new TimingProfile(
                "Vanilla",
                true,
                gameSettings!.dayDuration,
                gameSettings.daysBeforeQuota,
                gameSettings.startingQuota,
                gameSettings.catchUpFactor,
                (gameSettings.quotas ?? Array.Empty<float>()).ToArray());

            return true;
        }

        return TryResolveProfile(gameSettings!, context, out profile);
    }

    public static bool TryApplyManualOverrides(
        TimingProfile profile,
        string context,
        out IReadOnlyList<ValidationOutcome> outcomes)
    {
        EnsureInitialized();

        if (!TryGetActiveGameSettings(context, out var gameSettings))
        {
            outcomes = new[]
            {
                ValidationOutcome.Error(nameof(GameSettings), "No active GameSettings were available to apply manual overrides."),
            };

            return false;
        }

        var validation = TimingProfileValidator.Validate(profile);
        outcomes = validation;
        var errors = validation.Where(outcome => outcome.Status == ValidationStatus.Error).ToArray();
        if (errors.Length > 0)
        {
            foreach (var outcome in errors)
            {
                _log!.LogError($"[{context}] {outcome.TargetField}: {outcome.Message}");
            }

            return false;
        }

        _settings!.SetManualOverrides(profile);
        ApplyResolvedProfileToGameSettings(gameSettings!, profile, context);

        var gameManager = UnityEngine.Object.FindFirstObjectByType<GameManager>();
        if (gameManager != null)
        {
            ApplyResolvedProfileToGameManager(gameManager, profile, context);
        }

        var saveManager = UnityEngine.Object.FindFirstObjectByType<SaveManager>();
        if (saveManager != null)
        {
            var saveData = CurrentSaveDataRef(saveManager);
            if (saveData != null)
            {
                ApplyInitialQuotaToSaveData(saveData, profile, context);
            }
        }

        return true;
    }

    public static bool TryApplyToGameManagerRuntime(GameManager gameManager, string context)
    {
        EnsureInitialized();

        if (gameManager == null || !_settings!.IsEnabled)
        {
            return false;
        }

        var gameSettings = GameSettingsRef(gameManager);
        if (gameSettings == null)
        {
            return false;
        }

        if (!TryResolveProfile(gameSettings, context, out var resolvedProfile))
        {
            return false;
        }

        ApplyResolvedProfileToGameManager(gameManager, resolvedProfile!, context);
        return true;
    }

    private static bool TryResolveProfile(GameSettings gameSettings, string context, out TimingProfile? profile)
    {
        if (!_settings!.TryCreateResolvedProfile(gameSettings, out var resolvedProfile, out var outcomes))
        {
            foreach (var outcome in outcomes.Where(o => o.Status == ValidationStatus.Error))
            {
                _log!.LogError($"[{context}] {outcome.TargetField}: {outcome.Message}");
            }

            profile = null;
            return false;
        }

        profile = resolvedProfile;
        return true;
    }

    private static bool TryGetActiveGameSettings(
        string context,
        out GameSettings? gameSettings,
        bool includeSceneInstance = true)
    {
        if (includeSceneInstance)
        {
            var gameManager = UnityEngine.Object.FindFirstObjectByType<GameManager>();
            if (gameManager != null)
            {
                var sceneSettings = GameSettingsRef(gameManager);
                if (sceneSettings != null)
                {
                    gameSettings = sceneSettings;
                    return true;
                }
            }
        }

        gameSettings = Resources.Load<GameSettings>("GameSettings");
        if (gameSettings == null)
        {
            _log!.LogDebug($"No GameSettings resource was available during {context}.");
            return false;
        }

        return true;
    }

    private static void ApplyResolvedProfileToGameSettings(GameSettings gameSettings, TimingProfile profile, string context)
    {
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
    }

    private static void ApplyResolvedProfileToGameManager(GameManager gameManager, TimingProfile profile, string context)
    {
        if (!CanUpdatePreDayRuntimeState(gameManager))
        {
            return;
        }

        NetworkTimerProperty?.SetValue(gameManager, profile.DayDurationSeconds);
        NetworkCurrentQuotaProperty?.SetValue(gameManager, profile.StartingQuota);
        NetworkRequiredQuotaProperty?.SetValue(gameManager, profile.StartingQuota);

        var daysPassed = NetworkDaysPassedProperty?.GetValue(gameManager) as int? ?? 0;
        if (daysPassed == 0)
        {
            NetworkDaysLeftProperty?.SetValue(gameManager, profile.DaysBeforeQuota);
        }

        _log!.LogInfo(
            $"[{context}] Applied pre-day runtime state to GameManager " +
            $"(timer={profile.DayDurationSeconds}, currentQuota={profile.StartingQuota}, requiredQuota={profile.StartingQuota}, daysBeforeQuota={profile.DaysBeforeQuota}).");
    }

    private static void ApplyInitialQuotaToSaveData(SaveData saveData, TimingProfile profile, string context)
    {
        saveData.currentQuota = profile.StartingQuota;
        saveData.requiredQuotaToNextFloor = profile.StartingQuota;

        _log!.LogInfo(
            $"[{context}] Applied initial quota {profile.StartingQuota} to the active save state.");
    }

    private static bool CanUpdatePreDayRuntimeState(GameManager gameManager)
    {
        var hasDayStarted = HasDayStartedProperty?.GetValue(gameManager) as bool? ?? false;
        return !hasDayStarted;
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

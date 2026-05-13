using System;
using System.Collections.Generic;
using System.Linq;
using TimeConfig.Models;
using TimeConfig.Network;
using UnityEngine;

namespace TimeConfig.Runtime;

public static class NativeLobbySettingsMenu
{
    public const string SectionKey = "timeconfig.section";
    public const string DayDurationMinutesKey = "timeconfig.day-duration-minutes";
    public const string DaysBeforeQuotaKey = "timeconfig.days-before-quota";
    public const string StartingQuotaKey = "timeconfig.starting-quota";
    public const string CatchUpFactorKey = "timeconfig.catch-up-factor";

    private static SettingsLayout? _lobbySettingsLayout;
    private static bool _isSynchronizing;

    public static void RegisterLobbyLayout(SettingsLayout layout, string source)
    {
        if (layout == null)
        {
            return;
        }

        _lobbySettingsLayout = layout;
        PluginMain.Log.LogDebug($"[NativeLobbySettingsMenu] Registered lobby settings layout from {source}.");
    }

    public static bool EnsureInjected(SettingsLayout layout, string source)
    {
        if (layout == null)
        {
            return false;
        }

        if (!IsLobbySettingsLayout(layout))
        {
            return false;
        }

        RegisterLobbyLayout(layout, source);

        if (layout.tabs == null || layout.tabs.Count == 0)
        {
            PluginMain.Log.LogDebug($"[NativeLobbySettingsMenu] Skipped injection from {source} because the layout had no tabs.");
            return false;
        }

        var tab = FindTargetTab(layout);
        if (tab == null)
        {
            PluginMain.Log.LogDebug($"[NativeLobbySettingsMenu] Skipped injection from {source} because no lobby settings tab was found.");
            return false;
        }

        tab.entries ??= new List<SettingItemBase>();

        var addedEntries = 0;
        addedEntries += EnsureTitleEntry(tab.entries);
        addedEntries += EnsureSliderEntry(
            tab.entries,
            DayDurationMinutesKey,
            "Day duration (minutes)",
            1f,
            1440f,
            wholeNumbers: true);
        addedEntries += EnsureSliderEntry(
            tab.entries,
            DaysBeforeQuotaKey,
            "Days before quota",
            1f,
            30f,
            wholeNumbers: true);
        addedEntries += EnsureSliderEntry(
            tab.entries,
            StartingQuotaKey,
            "Starting quota",
            0f,
            100000f,
            wholeNumbers: true);
        addedEntries += EnsureSliderEntry(
            tab.entries,
            CatchUpFactorKey,
            "Catch-up factor",
            0f,
            5f,
            wholeNumbers: false);

        SyncFromCurrentState();

        PluginMain.Log.LogInfo(
            $"[NativeLobbySettingsMenu] {(addedEntries > 0 ? "Injected" : "Reused")} native lobby settings entries from {source}. Tab='{tab.tabName}', added={addedEntries}.");

        return true;
    }

    public static void RefreshFromLobbyButton(SettingsLayout layout)
    {
        EnsureInjected(layout, "LobbyModeDropdownButton.OnClick");
        SyncFromCurrentState();
    }

    public static void HandleSettingChanged(SettingItemBase entry)
    {
        if (_isSynchronizing || entry == null || !IsTimeConfigKey(entry.key) || _lobbySettingsLayout == null)
        {
            return;
        }

        if (!TryGetCurrentProfileForEditing(out var currentProfile) || currentProfile == null)
        {
            return;
        }

        var dayDurationEntry = FindEntry<SliderSettingItem>(_lobbySettingsLayout, DayDurationMinutesKey);
        var daysBeforeQuotaEntry = FindEntry<SliderSettingItem>(_lobbySettingsLayout, DaysBeforeQuotaKey);
        var startingQuotaEntry = FindEntry<SliderSettingItem>(_lobbySettingsLayout, StartingQuotaKey);
        var catchUpFactorEntry = FindEntry<SliderSettingItem>(_lobbySettingsLayout, CatchUpFactorKey);
        if (dayDurationEntry == null || daysBeforeQuotaEntry == null || startingQuotaEntry == null || catchUpFactorEntry == null)
        {
            return;
        }

        var updatedProfile = new TimingProfile(
            "ActiveConfig",
            false,
            dayDurationEntry.value * 60f,
            Mathf.RoundToInt(daysBeforeQuotaEntry.value),
            (long)Mathf.Round(startingQuotaEntry.value),
            catchUpFactorEntry.value,
            currentProfile.QuotaMultipliers);

        if (!TimingCoordinator.TryApplyManualOverrides(
                updatedProfile,
                "NativeLobbySettingsMenu.NotifyChanged",
                out _))
        {
            SyncFromCurrentState();
            return;
        }

        LobbyVisibility.BroadcastCurrentState();
        SyncFromCurrentState();
    }

    private static void SyncFromCurrentState()
    {
        if (_lobbySettingsLayout == null)
        {
            return;
        }

        if (!TryGetCurrentProfileForEditing(out var profile) || profile == null)
        {
            return;
        }

        var dayDurationEntry = FindEntry<SliderSettingItem>(_lobbySettingsLayout, DayDurationMinutesKey);
        var daysBeforeQuotaEntry = FindEntry<SliderSettingItem>(_lobbySettingsLayout, DaysBeforeQuotaKey);
        var startingQuotaEntry = FindEntry<SliderSettingItem>(_lobbySettingsLayout, StartingQuotaKey);
        var catchUpFactorEntry = FindEntry<SliderSettingItem>(_lobbySettingsLayout, CatchUpFactorKey);
        if (dayDurationEntry == null || daysBeforeQuotaEntry == null || startingQuotaEntry == null || catchUpFactorEntry == null)
        {
            return;
        }

        _isSynchronizing = true;
        try
        {
            dayDurationEntry.value = Mathf.Clamp(profile.DayDurationSeconds / 60f, dayDurationEntry.min, dayDurationEntry.max);
            dayDurationEntry.defaultValue = dayDurationEntry.value;

            daysBeforeQuotaEntry.value = Mathf.Clamp(profile.DaysBeforeQuota, daysBeforeQuotaEntry.min, daysBeforeQuotaEntry.max);
            daysBeforeQuotaEntry.defaultValue = daysBeforeQuotaEntry.value;

            startingQuotaEntry.max = Mathf.Max(100000f, Mathf.Ceil((float)profile.StartingQuota / 5000f) * 5000f);
            startingQuotaEntry.value = Mathf.Clamp((float)profile.StartingQuota, startingQuotaEntry.min, startingQuotaEntry.max);
            startingQuotaEntry.defaultValue = startingQuotaEntry.value;

            catchUpFactorEntry.value = Mathf.Clamp(profile.CatchUpFactor, catchUpFactorEntry.min, catchUpFactorEntry.max);
            catchUpFactorEntry.defaultValue = catchUpFactorEntry.value;
        }
        finally
        {
            _isSynchronizing = false;
        }
    }

    private static bool TryGetCurrentProfileForEditing(out TimingProfile? profile)
    {
        if (!TimingCoordinator.TryGetResolvedProfile("NativeLobbySettingsMenu.Sync", out profile) || profile == null)
        {
            return false;
        }

        return true;
    }

    private static bool IsTimeConfigKey(string? key) =>
        key == DayDurationMinutesKey ||
        key == DaysBeforeQuotaKey ||
        key == StartingQuotaKey ||
        key == CatchUpFactorKey;

    private static bool IsLobbySettingsLayout(SettingsLayout layout) =>
        FindTargetTab(layout) != null;

    private static SettingsLayout.Tab? FindTargetTab(SettingsLayout layout)
    {
        foreach (var tab in layout.tabs)
        {
            if (tab.entries == null || tab.entries.Count == 0)
            {
                continue;
            }

            if (tab.entries.Any(IsLobbyModeEntry))
            {
                return tab;
            }

            if (!string.IsNullOrWhiteSpace(tab.tabName) &&
                string.Equals(tab.tabName.Trim(), "Settings", StringComparison.OrdinalIgnoreCase))
            {
                return tab;
            }
        }

        return null;
    }

    private static bool IsLobbyModeEntry(SettingItemBase entry)
    {
        if (entry is not DropdownSettingItem)
        {
            return false;
        }

        if (string.Equals(entry.key, SectionKey, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return string.Equals(entry.label?.Trim(), "Lobby Mode", StringComparison.OrdinalIgnoreCase);
    }

    private static int EnsureTitleEntry(ICollection<SettingItemBase> entries)
    {
        if (entries.Any(entry => entry.key == SectionKey))
        {
            return 0;
        }

        var titleEntry = ScriptableObject.CreateInstance<TitleSettingItem>();
        titleEntry.hideFlags = HideFlags.HideAndDontSave;
        titleEntry.key = SectionKey;
        titleEntry.label = "TimeConfig";
        entries.Add(titleEntry);
        return 1;
    }

    private static int EnsureSliderEntry(
        ICollection<SettingItemBase> entries,
        string key,
        string label,
        float min,
        float max,
        bool wholeNumbers)
    {
        if (entries.Any(entry => entry.key == key))
        {
            return 0;
        }

        var sliderEntry = ScriptableObject.CreateInstance<SliderSettingItem>();
        sliderEntry.hideFlags = HideFlags.HideAndDontSave;
        sliderEntry.key = key;
        sliderEntry.label = label;
        sliderEntry.min = min;
        sliderEntry.max = max;
        sliderEntry.wholeNumbers = wholeNumbers;
        sliderEntry.value = min;
        sliderEntry.defaultValue = min;
        sliderEntry.loadOnSceneStart = false;
        entries.Add(sliderEntry);
        return 1;
    }

    private static T? FindEntry<T>(SettingsLayout layout, string key)
        where T : SettingItemBase
    {
        foreach (var tab in layout.tabs)
        {
            if (tab.entries == null)
            {
                continue;
            }

            var match = tab.entries.OfType<T>().FirstOrDefault(entry => entry.key == key);
            if (match != null)
            {
                return match;
            }
        }

        return null;
    }
}
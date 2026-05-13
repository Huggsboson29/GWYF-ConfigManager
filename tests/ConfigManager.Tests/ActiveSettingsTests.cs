using System;
using System.IO;
using BepInEx.Configuration;
using ConfigManager.Configuration;
using ConfigManager.Models;

namespace ConfigManager.Tests;

public sealed class ActiveSettingsTests
{
    private static string MakeTempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), "ConfigManagerTests_" + Path.GetRandomFileName());
        Directory.CreateDirectory(dir);
        return dir;
    }

    [Fact]
    public void TryCreateResolvedProfile_IgnoresDaysBeforeQuotaOverrides()
    {
        var dir = MakeTempDir();
        try
        {
            var configPath = Path.Combine(dir, "com.lncinteractive.cfg");
            var config = new ConfigFile(configPath, true);
            config.Bind("General", "EnableCustomTiming", true, string.Empty).Value = true;
            config.Bind("Timing", "DaysBeforeQuota", 20, string.Empty).Value = 20;

            var settings = ActiveSettings.Bind(config);
            var baseProfile = new TimingProfile(
                "Vanilla",
                true,
                300f,
                3,
                100L,
                0.75f,
                QuotaScalingMode.Vanilla,
                new[] { 1.2f, 1.3f });

            var success = settings.TryCreateResolvedProfile(baseProfile, out var resolvedProfile, out var outcomes);

            Assert.True(success);
            Assert.NotNull(resolvedProfile);
            Assert.Equal(3, resolvedProfile!.DaysBeforeQuota);
            Assert.DoesNotContain(outcomes, outcome => outcome.Status == ValidationStatus.Error);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void SetManualOverrides_ResetsDaysBeforeQuotaToPreserveValue()
    {
        var dir = MakeTempDir();
        try
        {
            var configPath = Path.Combine(dir, "com.lncinteractive.cfg");
            var config = new ConfigFile(configPath, true);
            var settings = ActiveSettings.Bind(config);
            var profile = new TimingProfile(
                "ActiveConfig",
                false,
                600f,
                10,
                200L,
                0.5f,
                QuotaScalingMode.CustomPattern,
                new[] { 1.1f, 1.3f });

            settings.SetManualOverrides(profile);

            var daysBeforeQuotaEntry = config.Bind("Timing", "DaysBeforeQuota", 0, string.Empty);
            Assert.Equal(ActiveSettings.PreserveInt, daysBeforeQuotaEntry.Value);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void Bind_SeedsEntries_WithFallbackVanillaDefaults_WhenNoConfigExists()
    {
        var dir = MakeTempDir();
        try
        {
            var configPath = Path.Combine(dir, "com.lncinteractive.cfg");
            var config = new ConfigFile(configPath, true);

            _ = ActiveSettings.Bind(config);

            Assert.Equal(ActiveSettings.FallbackVanillaDayDurationSeconds, config.Bind("Timing", "DayDurationSeconds", 0f, string.Empty).Value);
            Assert.Equal(ActiveSettings.FallbackVanillaDaysBeforeQuota, config.Bind("Timing", "DaysBeforeQuota", 0, string.Empty).Value);
            Assert.Equal(ActiveSettings.FallbackVanillaStartingQuota, config.Bind("Quota", "StartingQuota", 0L, string.Empty).Value);
            Assert.Equal(ActiveSettings.FallbackVanillaCatchUpFactor, config.Bind("Quota", "CatchUpFactor", 0f, string.Empty).Value);
            Assert.Equal(QuotaScalingMode.Vanilla.ToString(), config.Bind("Quota", "QuotaScalingMode", string.Empty, string.Empty).Value);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }
}
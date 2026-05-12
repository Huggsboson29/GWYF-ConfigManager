using System.IO;
using TimeConfig.Configuration;
using TimeConfig.Models;

namespace TimeConfig.Tests;

public sealed class ProfileStoreTests
{
    private static string MakeTempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), "TimeConfigTests_" + Path.GetRandomFileName());
        Directory.CreateDirectory(dir);
        return dir;
    }

    [Fact]
    public void SaveAndLoad_RoundTrips_AllFields()
    {
        var dir = MakeTempDir();
        try
        {
            var store = new ProfileStore(dir);
            var original = new TimingProfile(
                "EasyMode",
                false,
                600f,
                5,
                200L,
                0.5f,
                new[] { 1.1f, 1.3f, 1.5f, 2.0f });

            store.Save("EasyMode", original);

            Assert.True(store.TryLoad("EasyMode", out var loaded));
            Assert.NotNull(loaded);
            Assert.Equal(600f, loaded!.DayDurationSeconds);
            Assert.Equal(5, loaded.DaysBeforeQuota);
            Assert.Equal(200L, loaded.StartingQuota);
            Assert.Equal(0.5f, loaded.CatchUpFactor);
            Assert.Equal(new[] { 1.1f, 1.3f, 1.5f, 2.0f }, loaded.QuotaMultipliers);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void TryLoad_ReturnsFalse_WhenProfileMissing()
    {
        var dir = MakeTempDir();
        try
        {
            var store = new ProfileStore(dir);
            Assert.False(store.TryLoad("nonexistent", out _));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void ListProfiles_ReturnsNames_AlphabeticallySorted()
    {
        var dir = MakeTempDir();
        try
        {
            var store = new ProfileStore(dir);
            var dummy = new TimingProfile("x", false, 300f, 3, 100L, 0.75f, new[] { 1.2f });

            store.Save("Zebra", dummy);
            store.Save("Alpha", dummy);
            store.Save("Medium", dummy);

            var names = store.ListProfiles();
            Assert.Equal(new[] { "Alpha", "Medium", "Zebra" }, names);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void Delete_RemovesProfile_ReturnsTrue()
    {
        var dir = MakeTempDir();
        try
        {
            var store = new ProfileStore(dir);
            var dummy = new TimingProfile("x", false, 300f, 3, 100L, 0.75f, Array.Empty<float>());
            store.Save("ToDelete", dummy);

            Assert.True(store.Delete("ToDelete"));
            Assert.False(store.TryLoad("ToDelete", out _));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void Delete_ReturnsFalse_WhenProfileMissing()
    {
        var dir = MakeTempDir();
        try
        {
            var store = new ProfileStore(dir);
            Assert.False(store.Delete("doesNotExist"));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }
}

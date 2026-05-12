using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using TimeConfig.Models;

namespace TimeConfig.Configuration;

/// <summary>
/// Saves and loads named timing profiles from flat key=value files
/// stored under <c>&lt;configDir&gt;/TimeConfig/profiles/</c>.
/// </summary>
public sealed class ProfileStore
{
    private readonly string _profilesDir;

    public ProfileStore(string configDir)
    {
        _profilesDir = Path.Combine(configDir, "TimeConfig", "profiles");
    }

    public IReadOnlyList<string> ListProfiles()
    {
        if (!Directory.Exists(_profilesDir))
            return Array.Empty<string>();

        return Directory
            .GetFiles(_profilesDir, "*.profile")
            .Select(f => Path.GetFileNameWithoutExtension(f) ?? Path.GetFileName(f))
            .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public bool TryLoad(string name, out StoredProfile? profile)
    {
        profile = null;
        var path = ProfilePath(name);
        if (!File.Exists(path)) return false;

        var props = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var rawLine in File.ReadAllLines(path))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line[0] == '#') continue;
            var sep = line.IndexOf('=');
            if (sep < 1) continue;
            props[line[..sep].Trim()] = line[(sep + 1)..].Trim();
        }

        profile = new StoredProfile(
            ParseFloat(props, "DayDurationSeconds", ActiveSettings.PreserveFloat),
            ParseInt(props, "DaysBeforeQuota", ActiveSettings.PreserveInt),
            ParseLong(props, "StartingQuota", ActiveSettings.PreserveLong),
            ParseFloat(props, "CatchUpFactor", ActiveSettings.PreserveFloat),
            ParseMultipliers(props));

        return true;
    }

    public void Save(string name, TimingProfile profile)
    {
        Directory.CreateDirectory(_profilesDir);

        var lines = new List<string>
        {
            $"# TimeConfig profile: {name}",
            $"DayDurationSeconds={profile.DayDurationSeconds.ToString(CultureInfo.InvariantCulture)}",
            $"DaysBeforeQuota={profile.DaysBeforeQuota}",
            $"StartingQuota={profile.StartingQuota}",
            $"CatchUpFactor={profile.CatchUpFactor.ToString(CultureInfo.InvariantCulture)}",
        };

        if (profile.QuotaMultipliers.Count > 0)
        {
            lines.Add("QuotaMultipliers=" + string.Join(",",
                profile.QuotaMultipliers.Select(m => m.ToString(CultureInfo.InvariantCulture))));
        }

        File.WriteAllLines(path: ProfilePath(name), contents: lines);
    }

    public bool Delete(string name)
    {
        var path = ProfilePath(name);
        if (!File.Exists(path)) return false;
        File.Delete(path);
        return true;
    }

    private string ProfilePath(string name) =>
        Path.Combine(_profilesDir, $"{name}.profile");

    private static float ParseFloat(Dictionary<string, string> props, string key, float fallback) =>
        props.TryGetValue(key, out var raw)
        && float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var v)
            ? v : fallback;

    private static int ParseInt(Dictionary<string, string> props, string key, int fallback) =>
        props.TryGetValue(key, out var raw) && int.TryParse(raw, out var v) ? v : fallback;

    private static long ParseLong(Dictionary<string, string> props, string key, long fallback) =>
        props.TryGetValue(key, out var raw) && long.TryParse(raw, out var v) ? v : fallback;

    private static float[] ParseMultipliers(Dictionary<string, string> props)
    {
        if (!props.TryGetValue("QuotaMultipliers", out var raw)) return Array.Empty<float>();
        var tokens = raw.Split(',');
        var result = new float[tokens.Length];
        for (int i = 0; i < tokens.Length; i++)
        {
            if (!float.TryParse(tokens[i].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out result[i]))
                return Array.Empty<float>();
        }
        return result;
    }
}

/// <summary>
/// Raw values loaded from a named profile file.
/// Fields use the sentinel values from <see cref="ActiveSettings"/> to mean "preserve vanilla".
/// </summary>
public sealed class StoredProfile
{
    public StoredProfile(
        float dayDurationSeconds,
        int daysBeforeQuota,
        long startingQuota,
        float catchUpFactor,
        float[] quotaMultipliers)
    {
        DayDurationSeconds = dayDurationSeconds;
        DaysBeforeQuota = daysBeforeQuota;
        StartingQuota = startingQuota;
        CatchUpFactor = catchUpFactor;
        QuotaMultipliers = quotaMultipliers;
    }

    public float DayDurationSeconds { get; }
    public int DaysBeforeQuota { get; }
    public long StartingQuota { get; }
    public float CatchUpFactor { get; }
    public float[] QuotaMultipliers { get; }
}

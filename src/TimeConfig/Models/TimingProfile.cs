using System;
using System.Collections.Generic;

namespace TimeConfig.Models;

public sealed class TimingProfile
{
    public TimingProfile(
        string name,
        bool isVanillaProfile,
        float dayDurationSeconds,
        int daysBeforeQuota,
        long startingQuota,
        float catchUpFactor,
        IReadOnlyList<float> quotaMultipliers)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        IsVanillaProfile = isVanillaProfile;
        DayDurationSeconds = dayDurationSeconds;
        DaysBeforeQuota = daysBeforeQuota;
        StartingQuota = startingQuota;
        CatchUpFactor = catchUpFactor;
        QuotaMultipliers = quotaMultipliers ?? throw new ArgumentNullException(nameof(quotaMultipliers));
    }

    public string Name { get; }

    public bool IsVanillaProfile { get; }

    public float DayDurationSeconds { get; }

    public int DaysBeforeQuota { get; }

    public long StartingQuota { get; }

    public float CatchUpFactor { get; }

    public IReadOnlyList<float> QuotaMultipliers { get; }
}

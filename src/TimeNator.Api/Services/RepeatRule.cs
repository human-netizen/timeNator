namespace TimeNator.Api.Services;

/// <summary>
/// When a repeating to-do comes due. Three shapes only, stored as text:
/// "daily", "weekdays" (Monday to Friday), or "weekly:mon,wed,fri".
/// </summary>
public sealed record RepeatRule(IReadOnlySet<DayOfWeek> Days)
{
    private static readonly Dictionary<string, DayOfWeek> DayNames = new()
    {
        ["sun"] = DayOfWeek.Sunday, ["mon"] = DayOfWeek.Monday, ["tue"] = DayOfWeek.Tuesday,
        ["wed"] = DayOfWeek.Wednesday, ["thu"] = DayOfWeek.Thursday, ["fri"] = DayOfWeek.Friday,
        ["sat"] = DayOfWeek.Saturday
    };

    public static bool TryParse(string? text, out RepeatRule? rule)
    {
        rule = null;
        if (string.IsNullOrWhiteSpace(text))
            return false;

        var normalized = text.Trim().ToLowerInvariant();
        if (normalized == "daily")
        {
            rule = new RepeatRule(Enum.GetValues<DayOfWeek>().ToHashSet());
            return true;
        }
        if (normalized == "weekdays")
        {
            rule = new RepeatRule(new HashSet<DayOfWeek>
                { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday });
            return true;
        }
        if (!normalized.StartsWith("weekly:"))
            return false;

        var days = new HashSet<DayOfWeek>();
        foreach (var name in normalized["weekly:".Length..].Split(',', StringSplitOptions.TrimEntries))
        {
            if (!DayNames.TryGetValue(name, out var day))
                return false;
            days.Add(day);
        }
        if (days.Count == 0)
            return false;
        rule = new RepeatRule(days);
        return true;
    }

    /// <summary>Whether the rule, starting on <paramref name="start"/>, produces an occurrence on <paramref name="date"/>.</summary>
    public bool OccursOn(DateOnly date, DateOnly start) => date >= start && Days.Contains(date.DayOfWeek);

    /// <summary>Every occurrence from <paramref name="from"/> to <paramref name="to"/>, inclusive.</summary>
    public IEnumerable<DateOnly> Occurrences(DateOnly start, DateOnly from, DateOnly to)
    {
        for (var date = from < start ? start : from; date <= to; date = date.AddDays(1))
            if (Days.Contains(date.DayOfWeek))
                yield return date;
    }
}

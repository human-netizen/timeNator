using TimeNator.Api.Services;

namespace TimeNator.Api.Tests;

public class RepeatRuleTests
{
    // 2026-07-27 is a Monday.
    private static readonly DateOnly Monday = new(2026, 7, 27);

    [Theory]
    [InlineData("daily", 7)]
    [InlineData("weekdays", 5)]
    [InlineData("weekly:mon,wed,fri", 3)]
    [InlineData(" WEEKLY: sat , sun ", 2)]
    public void Occurrences_in_one_week(string text, int expected)
    {
        Assert.True(RepeatRule.TryParse(text, out var rule));

        Assert.Equal(expected, rule!.Occurrences(Monday, Monday, Monday.AddDays(6)).Count());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("hourly")]
    [InlineData("weekly:")]
    [InlineData("weekly:funday")]
    public void Rejects_anything_else(string? text)
    {
        Assert.False(RepeatRule.TryParse(text, out _));
    }

    [Fact]
    public void Nothing_occurs_before_the_start_date()
    {
        RepeatRule.TryParse("daily", out var rule);

        Assert.False(rule!.OccursOn(Monday.AddDays(-1), Monday));
        Assert.True(rule.OccursOn(Monday, Monday));
        Assert.Equal(Monday, rule.Occurrences(Monday, Monday.AddDays(-10), Monday).Single());
    }

    [Fact]
    public void Weekdays_skip_the_weekend()
    {
        RepeatRule.TryParse("weekdays", out var rule);

        Assert.True(rule!.OccursOn(Monday.AddDays(4), Monday));
        Assert.False(rule.OccursOn(Monday.AddDays(5), Monday));
        Assert.False(rule.OccursOn(Monday.AddDays(6), Monday));
    }
}

namespace TimeNator.Shared.Dtos;

/// <summary>
/// One row of the daily global leaderboard. <see cref="Rank"/> is sent explicitly rather
/// than inferred from position, because the same type is pushed in live updates where
/// position may not mean rank.
/// </summary>
public record LeaderboardEntry(Guid UserId, string DisplayName, string? AvatarKey, int DurationSeconds, int Rank);

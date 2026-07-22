using System.Text.Json.Serialization;

namespace TimeNator.Shared;

[JsonConverter(typeof(JsonStringEnumConverter<SessionMode>))]
public enum SessionMode
{
    Stopwatch,
    Countdown,
    Pomodoro
}

[JsonConverter(typeof(JsonStringEnumConverter<SessionSource>))]
public enum SessionSource
{
    Timer,
    Manual,
    Offline
}

[JsonConverter(typeof(JsonStringEnumConverter<GroupRole>))]
public enum GroupRole
{
    Owner,
    Member
}

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

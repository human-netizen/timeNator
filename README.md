# TimeNator

A Windows study timer with online study groups. Track focused work per subject,
keep distracting applications out of a study session, and study alongside other
people in real time.

Built as a .NET 10 API with an Avalonia desktop client.

## What it does

**Timer and sessions.** Per-subject stopwatch, countdown and pomodoro modes.
The timer pauses itself when the machine goes idle, tracks the longest
uninterrupted streak, and survives a crash: an in-flight session is journaled
to disk and resumed on the next start. Manual entry covers study done away from
the computer, and a history screen lists every session.

**Focus enforcement.** An allowed-application list backed by a Win32
foreground-window hook. When an application outside the list takes focus during
a session, TimeNator logs it, warns, or pulls itself back to the front,
depending on the chosen strictness. Every interruption is recorded to a
distraction log.

**Planner.** To-dos with repeat rules, a weekly timetable of fixed
commitments, D-Day countdowns, and a daily review pairing the day's sessions
with the day's to-dos.

**Groups.** Public groups, and private ones joined by password or invite code.
Members see live status: who is studying right now, on what subject, and since
when. Groups have chat, an announcement, and moderation (kick, ban, mute).

**Leaderboard.** One daily global ranking of every user by seconds studied,
top 50, updated live over SignalR.

**Desktop comforts.** Tray icon with close-to-tray, toast notifications with a
switch per kind, global hotkeys (Ctrl+Alt+S start or stop, Ctrl+Alt+P pause or
resume), optional start at Windows sign-in, desk mode, background noise, and
light and dark themes.

## Architecture

```
┌─────────────────────────────┐
│  TimeNator.Desktop          │
│  Avalonia, Windows          │
└──────┬──────────────────────┘
       │ HTTP + WebSocket
┌──────▼──────────────────────┐
│  TimeNator.Api              │
│  Controllers + StudyHub     │
└──────┬───────────────┬──────┘
       │               │
┌──────▼──────┐ ┌──────▼──────┐
│ PostgreSQL  │ │   Redis     │
│ (durable)   │ │ (ephemeral) │
└─────────────┘ └─────────────┘
```

The desktop client never references the API project. It talks HTTP and
WebSocket only, and both sides share `TimeNator.Shared`, which holds DTOs and
enums and references nothing, so the wire contract cannot drift.

The two data stores are split by a single question: does losing this matter?
Postgres holds everything durable: users, subjects, sessions, groups,
messages, planner items. Redis holds only what can be recomputed: current
presence and the daily leaderboard. If Redis is empty or down the application
still works; the leaderboard is rebuilt from Postgres.

Controllers stay thin. Business rules live in services that return a
`ServiceResult<T>`, which one extension maps to an HTTP response with
`ProblemDetails` for errors.

## Stack

| | |
|---|---|
| API | .NET 10, ASP.NET Core Web API (controllers), SignalR |
| Data | EF Core 10, Npgsql, PostgreSQL 18, Redis 8 |
| Auth | ASP.NET Core Identity, JWT bearer, rotating refresh tokens with reuse detection |
| Desktop | Avalonia 12.1, CommunityToolkit.Mvvm, P/Invoke to `user32.dll` |
| Testing | xUnit, `WebApplicationFactory`, Testcontainers |
| Other | Serilog, FluentValidation, ASP.NET Core rate limiting |

## Running it

Requires the .NET 10 SDK and Docker.

```bash
docker compose up -d          # Postgres on 5432, Redis on 6379
dotnet tool restore           # installs dotnet-ef at the pinned version
```

The API reads two secrets that are never committed. Set them once with user
secrets; the connection string matches the defaults in `docker-compose.yml`
(copy `.env.example` to `.env` to change those), and the signing key is any
random string of at least 32 bytes:

```bash
cd src/TimeNator.Api
dotnet user-secrets set "ConnectionStrings:Postgres" "Host=127.0.0.1;Port=5432;Database=timenator;Username=timenator;Password=timenator"
dotnet user-secrets set "Jwt:SigningKey" "<at least 32 random characters>"
dotnet ef database update
dotnet run
```

The API listens on port 5000. Then run the client from the repository root:

```bash
dotnet run --project src/TimeNator.Desktop
```

Tests start their own Postgres and Redis through Testcontainers, so Docker
must be running:

```bash
dotnet test
```

## Design decisions

**The client owns the clock.** A session is timed entirely on the desktop, and
the finished session is sent in one request when the user stops. The server
checks shape and sanity: a positive duration of at most 24 hours, no start or
end in the future, an end equal to the start plus studied and paused time, and
no overlap with the user's other sessions. It does not verify that the time was
really spent. That removes server-side heartbeat tracking and the state machine
that comes with it, and it lets the client keep timing through a network drop:
the session is journaled locally and the upload is retried with backoff.

The cost is that study time is self-reported, and a determined user could forge
a session by calling the API directly. The sanity rules cap the damage at an
implausible-looking day rather than an arbitrary number. This is a deliberate
trade: the leaderboard is a motivational display, not a competition with
stakes.

**Presence comes from the connection, not from heartbeats.** The client holds a
SignalR connection and announces when a timer starts and stops, and
`OnDisconnectedAsync` clears presence the same way when the connection drops.
After a reconnect the client announces again. Redis presence keys carry a
5-minute TTL only as a safety net for a server restart that never saw the
disconnect, which is the main reason presence lives in Redis: expiry is free
there, whereas in Postgres it would need a sweep job for rows that live for
minutes.

**Focus is enforced with a Win32 event hook, not polling.** `SetWinEventHook`
with `EVENT_SYSTEM_FOREGROUND` makes Windows report each foreground change as
it happens, so nothing wakes up every second to ask which window is active.
Pulling TimeNator back to the front is harder than it sounds: Windows refuses
`SetForegroundWindow` from a process that does not own the foreground, so the
client briefly attaches to the foreground thread's input queue, the documented
way to be allowed to take focus. Every `DllImport` lives in one `Interop`
folder, and each wrapper does nothing off Windows.

**The leaderboard is a Redis sorted set.** Ranking every user for a day is an
aggregate query in Postgres and a `ZINCRBY` per saved session plus one
`ZREVRANGE` in Redis. Only ids and scores live in the set; display names come
from Postgres by primary key, so a name change is never written twice. On a
cache miss the day is recomputed from Postgres.

## Known limitations

- Study time is self-reported and forgeable, as described above.
- Groups have one owner and no ownership transfer. The owner cannot leave,
  only delete the group.
- The API address is fixed at `http://localhost:5000`; there is no deployment
  configuration for the client.
- Windows only. The client builds elsewhere, but idle detection, focus
  enforcement and hotkeys use Win32 APIs and are inert on other platforms.

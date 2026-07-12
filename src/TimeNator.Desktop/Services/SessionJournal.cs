using System.Text.Json;
using TimeNator.Desktop.Models;
using TimeNator.Shared;
using TimeNator.Shared.Dtos;

namespace TimeNator.Desktop.Services;

public record ActiveSession(
    Guid SubjectId,
    string SubjectName,
    SessionMode Mode,
    TimerSnapshot Timer,
    DateTimeOffset LastSeenAt);

public record JournalContents(ActiveSession? Active, List<CreateSessionRequest> Pending);

/// <summary>
/// The one piece of local persistence: the running session, rewritten on every state
/// change and periodically while it runs, plus finished sessions the server has not
/// accepted yet. Not a sync queue; it holds at most a handful of sessions.
/// </summary>
public class SessionJournal
{
    private readonly string _path = Path.Combine(AppPaths.DataDirectory, "session.json");
    private readonly Lock _gate = new();
    private JournalContents? _contents;

    public JournalContents Read()
    {
        lock (_gate)
            return _contents ??= Load();
    }

    public void SetActive(ActiveSession? active)
    {
        lock (_gate)
            Write(Read() with { Active = active });
    }

    public void AddPending(CreateSessionRequest request)
    {
        lock (_gate)
            Write(Read() with { Pending = [.. Read().Pending, request] });
    }

    public void RemovePending(CreateSessionRequest request)
    {
        lock (_gate)
            Write(Read() with { Pending = Read().Pending.Where(p => p != request).ToList() });
    }

    private JournalContents Load()
    {
        try
        {
            if (File.Exists(_path))
                return JsonSerializer.Deserialize<JournalContents>(File.ReadAllText(_path))
                       ?? new JournalContents(null, []);
        }
        catch (Exception ex) when (ex is JsonException or IOException)
        {
            // A torn or unreadable journal is not worth crashing over; start clean.
        }
        return new JournalContents(null, []);
    }

    private void Write(JournalContents contents)
    {
        _contents = contents;
        Directory.CreateDirectory(AppPaths.DataDirectory);
        var temp = _path + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(contents));
        File.Move(temp, _path, overwrite: true);
    }
}

using System.Collections.ObjectModel;
using TimeNator.Shared.Dtos;

namespace TimeNator.Desktop.Services;

/// <summary>
/// The signed-in user's active subjects, shared by every screen that lists them so
/// a subject added on one screen appears on the others without a reload.
/// </summary>
public class SubjectCatalog(IApiClient api)
{
    public ObservableCollection<SubjectResponse> Subjects { get; } = [];

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        var subjects = await api.GetSubjectsAsync(cancellationToken);
        Subjects.Clear();
        foreach (var subject in subjects)
            Subjects.Add(subject);
    }

    public async Task<SubjectResponse> AddAsync(string name, string colorHex,
        CancellationToken cancellationToken = default)
    {
        var created = await api.CreateSubjectAsync(new CreateSubjectRequest(name, colorHex), cancellationToken);
        Subjects.Add(created);
        return created;
    }

    public async Task ArchiveAsync(SubjectResponse subject, CancellationToken cancellationToken = default)
    {
        await api.DeleteSubjectAsync(subject.Id, cancellationToken);
        Subjects.Remove(subject);
    }
}

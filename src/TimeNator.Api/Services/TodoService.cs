using Microsoft.EntityFrameworkCore;
using TimeNator.Api.Data;
using TimeNator.Api.Entities;
using TimeNator.Shared.Dtos;

namespace TimeNator.Api.Services;

public class TodoService(AppDbContext db, TimeProvider clock)
{
    /// <summary>
    /// The to-dos for one day: those due that day, undated ones still open, and one copy of
    /// every repeating to-do that applies. Copies are created the first time a day is read,
    /// so each day's copy can be ticked off on its own.
    /// </summary>
    public async Task<List<TodoResponse>> ListForDayAsync(Guid userId, DateOnly date,
        CancellationToken cancellationToken)
    {
        await MaterialiseRepeatsAsync(userId, date, cancellationToken);

        return await db.TodoItems
            .Where(t => t.UserId == userId && t.RepeatRule == null
                        && (t.DueDate == date || (t.DueDate == null && !t.IsDone)))
            .OrderBy(t => t.IsDone).ThenBy(t => t.CreatedAt)
            .Select(t => ToResponse(t))
            .ToListAsync(cancellationToken);
    }

    public async Task<ServiceResult<TodoResponse>> CreateAsync(Guid userId, CreateTodoRequest request,
        CancellationToken cancellationToken)
    {
        if (request.RepeatRule is not null)
        {
            if (!RepeatRule.TryParse(request.RepeatRule, out _))
                return Invalid("Repeat must be daily, weekdays, or weekly:mon,wed,...");
            if (request.DueDate is null)
                return Invalid("A repeating to-do needs a start date.");
        }
        if (await CheckSubjectAsync(userId, request.SubjectId, cancellationToken) is { } error)
            return error;

        var todo = new TodoItem
        {
            UserId = userId,
            Title = request.Title.Trim(),
            SubjectId = request.SubjectId,
            DueDate = request.DueDate,
            RepeatRule = request.RepeatRule?.Trim().ToLowerInvariant(),
            CreatedAt = clock.GetUtcNow()
        };
        db.TodoItems.Add(todo);
        await db.SaveChangesAsync(cancellationToken);
        return ServiceResult<TodoResponse>.Ok(await FindAsync(todo.Id, cancellationToken));
    }

    public async Task<ServiceResult<TodoResponse>> UpdateAsync(Guid userId, Guid id, UpdateTodoRequest request,
        CancellationToken cancellationToken)
    {
        var todo = await db.TodoItems.SingleOrDefaultAsync(t => t.Id == id && t.UserId == userId, cancellationToken);
        if (todo is null)
            return ServiceResult<TodoResponse>.Fail(ServiceError.NotFound, "To-do not found.");
        if (await CheckSubjectAsync(userId, request.SubjectId, cancellationToken) is { } error)
            return error;

        todo.Title = request.Title.Trim();
        todo.SubjectId = request.SubjectId;
        if (todo.RepeatParentId is null)
            todo.DueDate = request.DueDate; // A day's copy of a repeat stays on its day.
        if (todo.IsDone != request.IsDone)
            todo.CompletedAt = request.IsDone ? clock.GetUtcNow() : null;
        todo.IsDone = request.IsDone;
        await db.SaveChangesAsync(cancellationToken);
        return ServiceResult<TodoResponse>.Ok(await FindAsync(todo.Id, cancellationToken));
    }

    /// <summary>Deleting a repeating to-do deletes its copies too, through the cascade.</summary>
    public async Task<ServiceResult<bool>> DeleteAsync(Guid userId, Guid id, CancellationToken cancellationToken) =>
        await db.TodoItems.Where(t => t.Id == id && t.UserId == userId).ExecuteDeleteAsync(cancellationToken) > 0
            ? ServiceResult<bool>.Ok(true)
            : ServiceResult<bool>.Fail(ServiceError.NotFound, "To-do not found.");

    /// <summary>The repeating templates themselves, for managing them rather than a day's copies.</summary>
    public Task<List<TodoResponse>> ListRepeatingAsync(Guid userId, CancellationToken cancellationToken) =>
        db.TodoItems
            .Where(t => t.UserId == userId && t.RepeatRule != null)
            .OrderBy(t => t.Title)
            .Select(t => ToResponse(t))
            .ToListAsync(cancellationToken);

    private async Task MaterialiseRepeatsAsync(Guid userId, DateOnly date, CancellationToken cancellationToken)
    {
        var templates = await db.TodoItems
            .Where(t => t.UserId == userId && t.RepeatRule != null && t.DueDate <= date)
            .ToListAsync(cancellationToken);
        var existing = await db.TodoItems
            .Where(t => t.UserId == userId && t.RepeatParentId != null && t.DueDate == date)
            .Select(t => t.RepeatParentId!.Value)
            .ToListAsync(cancellationToken);

        var now = clock.GetUtcNow();
        foreach (var template in templates.Where(t => !existing.Contains(t.Id)))
        {
            if (!RepeatRule.TryParse(template.RepeatRule, out var rule) || !rule!.OccursOn(date, template.DueDate!.Value))
                continue;
            db.TodoItems.Add(new TodoItem
            {
                UserId = userId,
                Title = template.Title,
                SubjectId = template.SubjectId,
                DueDate = date,
                RepeatParentId = template.Id,
                CreatedAt = now
            });
        }

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Two reads of the same day raced; the unique index kept one copy, which is all we need.
            db.ChangeTracker.Clear();
        }
    }

    private async Task<ServiceResult<TodoResponse>?> CheckSubjectAsync(Guid userId, Guid? subjectId,
        CancellationToken cancellationToken) =>
        subjectId is { } id && !await db.Subjects.AnyAsync(s => s.Id == id && s.UserId == userId, cancellationToken)
            ? Invalid("Unknown subject.")
            : null;

    private Task<TodoResponse> FindAsync(Guid id, CancellationToken cancellationToken) =>
        db.TodoItems.Where(t => t.Id == id).Select(t => ToResponse(t)).SingleAsync(cancellationToken);

    private static ServiceResult<TodoResponse> Invalid(string message) =>
        ServiceResult<TodoResponse>.Fail(ServiceError.Invalid, message);

    private static TodoResponse ToResponse(TodoItem t) =>
        new(t.Id, t.Title, t.SubjectId, t.Subject != null ? t.Subject.Name : null, t.DueDate, t.IsDone,
            t.CompletedAt, t.RepeatRule, t.RepeatParentId);
}

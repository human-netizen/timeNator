using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TimeNator.Api.Services;
using TimeNator.Shared.Dtos;

namespace TimeNator.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/todos")]
public class TodosController(TodoService todos) : ControllerBase
{
    [HttpGet]
    public Task<List<TodoResponse>> List([FromQuery] DateOnly date, CancellationToken cancellationToken) =>
        todos.ListForDayAsync(User.GetUserId(), date, cancellationToken);

    [HttpGet("repeating")]
    public Task<List<TodoResponse>> Repeating(CancellationToken cancellationToken) =>
        todos.ListRepeatingAsync(User.GetUserId(), cancellationToken);

    [HttpPost]
    public async Task<ActionResult<TodoResponse>> Create(CreateTodoRequest request,
        CancellationToken cancellationToken)
    {
        if (!TitleIsValid(request.Title))
            return InvalidTitle();
        return this.ToActionResult(await todos.CreateAsync(User.GetUserId(), request, cancellationToken),
            created => Created($"/api/todos/{created.Id}", created));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<TodoResponse>> Update(Guid id, UpdateTodoRequest request,
        CancellationToken cancellationToken)
    {
        if (!TitleIsValid(request.Title))
            return InvalidTitle();
        return this.ToActionResult(await todos.UpdateAsync(User.GetUserId(), id, request, cancellationToken));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken) =>
        this.ToNoContent(await todos.DeleteAsync(User.GetUserId(), id, cancellationToken));

    private static bool TitleIsValid(string? title) => title?.Trim().Length is > 0 and <= 200;

    private ActionResult InvalidTitle()
    {
        ModelState.AddModelError("Title", "The title must be 1 to 200 characters.");
        return ValidationProblem(ModelState);
    }
}

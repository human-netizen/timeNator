using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TimeNator.Api.Services;
using TimeNator.Api.Validation;
using TimeNator.Shared.Dtos;

namespace TimeNator.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/subjects")]
public class SubjectsController(SubjectService subjects) : ControllerBase
{
    [HttpGet]
    public Task<List<SubjectResponse>> List(CancellationToken cancellationToken) =>
        subjects.ListAsync(User.GetUserId(), cancellationToken);

    [HttpPost]
    public async Task<ActionResult<SubjectResponse>> Create(
        CreateSubjectRequest request, IValidator<CreateSubjectRequest> validator, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return ValidationProblem(validation.ToModelState());

        var userId = User.GetUserId();
        if (await subjects.NameTakenAsync(userId, request.Name.Trim(), null, cancellationToken))
            return Problem(statusCode: StatusCodes.Status409Conflict, title: "A subject with this name exists.");

        var created = await subjects.CreateAsync(userId, request, cancellationToken);
        return Created($"/api/subjects/{created.Id}", created);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<SubjectResponse>> Update(
        Guid id, UpdateSubjectRequest request, IValidator<UpdateSubjectRequest> validator,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return ValidationProblem(validation.ToModelState());

        var userId = User.GetUserId();
        if (await subjects.NameTakenAsync(userId, request.Name.Trim(), id, cancellationToken))
            return Problem(statusCode: StatusCodes.Status409Conflict, title: "A subject with this name exists.");

        var updated = await subjects.UpdateAsync(userId, id, request, cancellationToken);
        return updated is null ? NotFound() : updated;
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken) =>
        await subjects.ArchiveAsync(User.GetUserId(), id, cancellationToken) ? NoContent() : NotFound();
}

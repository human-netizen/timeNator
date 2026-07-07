using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TimeNator.Api.Entities;
using TimeNator.Api.Services;
using TimeNator.Api.Validation;
using TimeNator.Shared.Dtos;

namespace TimeNator.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(
    UserManager<ApplicationUser> users,
    TokenService tokens,
    TimeProvider clock) : ControllerBase
{
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(
        RegisterRequest request, IValidator<RegisterRequest> validator, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return ValidationProblem(validation.ToModelState());

        var user = new ApplicationUser
        {
            Id = Guid.CreateVersion7(),
            UserName = request.Email,
            Email = request.Email,
            DisplayName = request.DisplayName.Trim(),
            CreatedAt = clock.GetUtcNow()
        };

        var result = await users.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(error.Code, error.Description);
            return ValidationProblem(ModelState);
        }

        return await IssueAsync(user, cancellationToken);
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var user = await users.FindByEmailAsync(request.Email);
        if (user is null || !await users.CheckPasswordAsync(user, request.Password))
            return Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Invalid email or password.");

        return await IssueAsync(user, cancellationToken);
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponse>> Refresh(RefreshRequest request, CancellationToken cancellationToken)
    {
        var refreshed = await tokens.RefreshAsync(request.RefreshToken, cancellationToken);
        if (refreshed is not var (user, issued))
            return Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Invalid refresh token.");

        return ToResponse(user, issued);
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(RefreshRequest request, CancellationToken cancellationToken)
    {
        await tokens.RevokeAsync(User.GetUserId(), request.RefreshToken, cancellationToken);
        return NoContent();
    }

    private async Task<AuthResponse> IssueAsync(ApplicationUser user, CancellationToken cancellationToken) =>
        ToResponse(user, await tokens.IssueAsync(user, cancellationToken));

    private static AuthResponse ToResponse(ApplicationUser user, IssuedTokens issued) =>
        new(user.Id, user.DisplayName, issued.AccessToken, issued.AccessTokenExpiresAt, issued.RefreshToken);
}

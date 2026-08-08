using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TimeNator.Api.Entities;
using TimeNator.Shared;
using TimeNator.Shared.Dtos;

namespace TimeNator.Api.Controllers;

/// <summary>The signed-in user's own profile. Identity's UserManager is the service here.</summary>
[ApiController]
[Authorize]
[Route("api/me")]
public class MeController(UserManager<ApplicationUser> users) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ProfileResponse>> Get()
    {
        var user = await users.FindByIdAsync(User.GetUserId().ToString());
        return user is null ? NotFound() : ToResponse(user);
    }

    [HttpPut]
    public async Task<ActionResult<ProfileResponse>> Update(UpdateProfileRequest request)
    {
        if (request.DisplayName?.Trim().Length is not (> 0 and <= 50))
            ModelState.AddModelError(nameof(request.DisplayName), "The display name must be 1 to 50 characters.");
        if (request.StatusMessage is { Length: > 200 })
            ModelState.AddModelError(nameof(request.StatusMessage), "The status must be at most 200 characters.");
        if (request.AvatarKey is not null && !Avatars.Glyphs.ContainsKey(request.AvatarKey))
            ModelState.AddModelError(nameof(request.AvatarKey), "Unknown avatar.");
        if (request.ThemeKey is not null && !Themes.All.Contains(request.ThemeKey))
            ModelState.AddModelError(nameof(request.ThemeKey), "Unknown theme.");
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var user = await users.FindByIdAsync(User.GetUserId().ToString());
        if (user is null)
            return NotFound();

        user.DisplayName = request.DisplayName!.Trim();
        user.StatusMessage = string.IsNullOrWhiteSpace(request.StatusMessage) ? null : request.StatusMessage.Trim();
        user.AvatarKey = request.AvatarKey;
        user.ThemeKey = request.ThemeKey;
        await users.UpdateAsync(user);
        return ToResponse(user);
    }

    private static ProfileResponse ToResponse(ApplicationUser u) =>
        new(u.Id, u.Email!, u.DisplayName, u.StatusMessage, u.AvatarKey, u.ThemeKey);
}

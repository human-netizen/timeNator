using Microsoft.AspNetCore.Mvc;
using TimeNator.Api.Services;

namespace TimeNator.Api.Controllers;

public static class ServiceResultExtensions
{
    public static ActionResult<T> ToActionResult<T>(this ControllerBase controller, ServiceResult<T> result,
        Func<T, ActionResult<T>>? onSuccess = null)
    {
        if (result.Error is { } error)
            return controller.ToProblem(error, result.Message);
        return onSuccess is null ? controller.Ok(result.Value) : onSuccess(result.Value!);
    }

    public static IActionResult ToNoContent<T>(this ControllerBase controller, ServiceResult<T> result) =>
        result.Error is { } error ? controller.ToProblem(error, result.Message) : controller.NoContent();

    /// <summary>The problem response for a failed result.</summary>
    public static ObjectResult ToProblem<T>(this ControllerBase controller, ServiceResult<T> failed) =>
        controller.ToProblem(failed.Error!.Value, failed.Message);

    private static ObjectResult ToProblem(this ControllerBase controller, ServiceError error, string? message) =>
        controller.Problem(
            statusCode: error switch
            {
                ServiceError.NotFound => StatusCodes.Status404NotFound,
                ServiceError.Forbidden => StatusCodes.Status403Forbidden,
                ServiceError.Conflict => StatusCodes.Status409Conflict,
                _ => StatusCodes.Status400BadRequest
            },
            title: message);
}

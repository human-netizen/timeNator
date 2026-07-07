using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace TimeNator.Api.Validation;

public static class ValidationExtensions
{
    public static ModelStateDictionary ToModelState(this ValidationResult result)
    {
        var modelState = new ModelStateDictionary();
        foreach (var error in result.Errors)
            modelState.AddModelError(error.PropertyName, error.ErrorMessage);
        return modelState;
    }
}

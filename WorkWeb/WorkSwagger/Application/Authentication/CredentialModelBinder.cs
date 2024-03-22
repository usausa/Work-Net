namespace WorkSwagger.Application.Authentication;

using Microsoft.AspNetCore.Mvc.ModelBinding;

public sealed class CredentialModelBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        bindingContext.Result = ModelBindingResult.Success(CredentialHelper.Create(bindingContext.HttpContext));
        return Task.CompletedTask;
    }
}

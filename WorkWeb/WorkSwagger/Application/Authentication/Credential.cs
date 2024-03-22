namespace WorkSwagger.Application.Authentication;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

[ModelBinder(typeof(CredentialModelBinder))]
[ValidateNever]
public sealed class Credential
{
    public string ClientId { get; }

    public Credential(string clientId)
    {
        ClientId = clientId;
    }
}

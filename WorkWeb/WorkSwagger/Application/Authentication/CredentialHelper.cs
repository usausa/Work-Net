namespace WorkSwagger.Application.Authentication;

using WorkSwagger.Application;

public static class CredentialHelper
{
    public static Credential Create(HttpContext httpContext)
    {
        var clientId = httpContext.Request.Headers[ExtensionHeaders.ClientId];
        return new Credential(clientId!);
    }
}

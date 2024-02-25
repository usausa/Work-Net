namespace WorkExceptionMvc.Areas.Default;

using Microsoft.AspNetCore.Mvc;

[Area("default")]
[Route("[controller]/[action]")]
[ResponseCache(Location = ResponseCacheLocation.None, NoStore = true, Duration = 0)]
[ApiExplorerSettings(IgnoreApi = true)]
public abstract class BaseDefaultController : Controller
{
}

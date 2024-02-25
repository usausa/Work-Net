namespace WorkExceptionMvc.Areas.Default.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using System.Diagnostics;

using WorkExceptionMvc.Areas.Default.Models;

[AllowAnonymous]
[Area("default")]
[ResponseCache(Location = ResponseCacheLocation.None, NoStore = true, Duration = 0)]
[ApiExplorerSettings(IgnoreApi = true)]
public sealed class ErrorController : Controller
{
    [Route("~/[controller]/{statusCode:int}")]
#pragma warning disable CA5395
    public IActionResult Index(int statusCode)
    {
        return View(new ErrorViewModel
        {
            StatusCode = statusCode,
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
        });
    }
#pragma warning restore CA5395
}

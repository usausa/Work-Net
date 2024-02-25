namespace WorkExceptionMvc.Areas.Api.Controllers;

using Microsoft.AspNetCore.Mvc;

public sealed class TestController : BaseApiController
{
    [HttpGet]
    public IActionResult Time()
    {
        return Ok(DateTime.Now);
    }

    [HttpGet]
    public IActionResult Exception() => throw new InvalidOperationException("Cause exception.");

    [HttpGet]
    public IActionResult Nothing() => NotFound();
}

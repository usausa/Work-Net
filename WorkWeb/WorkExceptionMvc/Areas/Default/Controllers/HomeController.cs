namespace WorkExceptionMvc.Areas.Default.Controllers;

using Microsoft.AspNetCore.Mvc;

public class HomeController : BaseDefaultController
{
    [Route("~/")]
    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    [HttpGet]
    public IActionResult Exception() => throw new InvalidOperationException("Cause exception.");

    [HttpGet]
    public IActionResult Nothing() => NotFound();
}

namespace WorkStartup.Controllers;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("[controller]/[action]")]
public class TestController : ControllerBase
{
    [HttpGet]
    public IActionResult Execute()
    {
        return Ok("success");
    }
}

namespace WorkLogHttp.Controllers;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]/[action]")]
public class TestController : ControllerBase
{
    [HttpGet]
    public IActionResult Success()
    {
        return Ok("success");
    }
}

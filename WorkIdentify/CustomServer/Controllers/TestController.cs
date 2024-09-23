namespace CustomServer.Controllers;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("[controller]/[action]")]
public class TestController : ControllerBase
{
    // TODO Smart & lower
    [HttpPost]
    public IActionResult Token()
    {
        return Ok();
    }
}

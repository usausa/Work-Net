namespace WorkApi.Controllers;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("[controller]/[action]")]
public class TestController : ControllerBase
{

    [HttpPost]
    public IActionResult Execute()
    {
        return Ok();
    }
}

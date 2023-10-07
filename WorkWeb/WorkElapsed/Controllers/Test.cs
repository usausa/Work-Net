namespace WorkTiming.Controllers;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("[controller]/[action]")]
public class Test : ControllerBase
{
    [HttpGet]
    public async ValueTask<IActionResult> Execute()
    {
        await Task.Delay(100);

        return Ok();
    }
}

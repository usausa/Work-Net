namespace WorkVersion.Controllers.V2;

using Asp.Versioning;

using Microsoft.AspNetCore.Mvc;

[ApiController]
[ApiVersion(2.0)]
[Route("api/v{version:apiVersion}/[controller]/[action]")]
public class TestController : ControllerBase
{
    [HttpGet]
    public IActionResult Execute()
    {
        return Ok("v2");
    }
}

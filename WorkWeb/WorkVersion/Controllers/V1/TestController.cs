namespace WorkVersion.Controllers.V1;

using Asp.Versioning;

using Microsoft.AspNetCore.Mvc;

[ApiController]
[ApiVersion(1.0, Deprecated = true)]
[Route("api/v{version:apiVersion}/[controller]/[action]")]
public class TestController : ControllerBase
{
    [HttpGet]
    public IActionResult Execute()
    {
        return Ok("v1");
    }
}

namespace WorkPipeline.Controllers;

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

    [HttpGet]
    public IActionResult Error()
    {
        throw new Exception("test");
    }
}

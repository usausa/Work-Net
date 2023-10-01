namespace WorkWebLogContext.Controllers;

using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("[controller]/[action]")]
public class TestController : ControllerBase
{
    private readonly ILogger<TestController> logger;

    public TestController(ILogger<TestController> logger)
    {
        this.logger = logger;
    }

    [HttpGet]
    public IActionResult Info()
    {
        logger.LogInformation("Info.");

        return Ok();
    }

    [HttpGet]
    public IActionResult Warning()
    {
        logger.LogWarning("Warning.");

        return Ok();
    }
}

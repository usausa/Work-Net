namespace WorkLokiWeb.Controllers;

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
        logger.LogInformation("Information message.");
        return Ok();
    }

    [HttpGet]
    public IActionResult Warning()
    {
        logger.LogWarning("Warning message.");
        return Ok();
    }

    [HttpGet]
    public IActionResult Exception()
    {
        throw new Exception("Raise exception.");
    }
}

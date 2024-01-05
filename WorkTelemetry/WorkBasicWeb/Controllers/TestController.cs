namespace WorkBasicWeb.Controllers;

using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("[controller]/[action]")]
public class TestController : ControllerBase
{
    private readonly ILogger<TestController> log;

    private readonly ApiMetrics metrics;

    public TestController(ILogger<TestController> log, ApiMetrics metrics)
    {
        this.log = log;
        this.metrics = metrics;
    }

    [HttpGet]
    public IActionResult Execute()
    {
        log.LogInformation("test execute.");
        metrics.IncrementTestExecute();

        return Ok("success");
    }


    [HttpGet]
    public IActionResult Collect()
    {
        System.GC.Collect();

        return Ok("success");
    }
}

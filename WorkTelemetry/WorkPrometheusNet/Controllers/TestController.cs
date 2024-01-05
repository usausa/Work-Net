namespace WorkPrometheusNet.Controllers;

using Microsoft.AspNetCore.Mvc;

using Prometheus;

public static class ApiMetrics
{
    private static readonly Counter TestExecute =
        Metrics.CreateCounter("api_test_execute", "Test controller execute.");

    public static void IncrementTestExecute() => TestExecute.Inc();
}

[ApiController]
[Route("[controller]/[action]")]
public class TestController : ControllerBase
{
    [HttpGet]
    public IActionResult Execute()
    {
        ApiMetrics.IncrementTestExecute();

        return Ok("success");
    }
}

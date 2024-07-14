namespace WorkIntegrationTest.Web.Controllers;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

public class Request
{
    public string Value { get; set; } = default!;
}

public class Response
{
    public string Value { get; set; } = default!;

    public int Counter { get; set; }
}

[ApiController]
[Route("[controller]/[action]")]
public class TestController : ControllerBase
{
    private ILogger<TestController> log;

    private readonly Settings settings;

    public TestController(ILogger<TestController> log, IOptions<Settings> settings)
    {
        this.log = log;
        this.settings = settings.Value;
    }

    [HttpPost]
    public IActionResult Execute([FromBody] Request request)
    {
        log.LogInformation("**** Test ****");

        return Ok(new Response { Value = request.Value, Counter = settings.Counter });
    }
}

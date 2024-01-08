namespace WorkBasicWeb.Controllers;

using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("[controller]/[action]")]
public class TestController : ControllerBase
{
    private readonly ILogger<TestController> log;

    private readonly ApiInstrument instrument;

    public TestController(ILogger<TestController> log, ApiInstrument instrument)
    {
        this.log = log;
        this.instrument = instrument;
    }

    [HttpGet]
    public IActionResult Execute()
    {
        log.LogInformation("test execute.");
        instrument.IncrementTestExecute();

        return Ok("success");
    }


    [HttpGet]
    public IActionResult Collect()
    {
        System.GC.Collect();

        return Ok("success");
    }
}

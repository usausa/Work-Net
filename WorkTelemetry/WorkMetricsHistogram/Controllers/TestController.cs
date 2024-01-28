namespace WorkMetricsHistogram.Controllers;

using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("[controller]/[action]")]
public class TestController : ControllerBase
{
    private readonly ApiInstrument instrument;

    public TestController(ApiInstrument instrument)
    {
        this.instrument = instrument;
    }

    [HttpGet]
    public IActionResult Execute()
    {
        var value = Random.Shared.NextDouble();
        instrument.RecordTest(value * 40);

        return Ok("success");
    }
}

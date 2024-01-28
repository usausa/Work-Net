namespace WorkMetricsTag.Controllers;

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
        var value = Random.Shared.Next(6);
        instrument.IncrementTest(((char)('C' + value)).ToString());

        return Ok("success");
    }


    [HttpGet]
    public IActionResult Collect()
    {
        System.GC.Collect();

        return Ok("success");
    }
}

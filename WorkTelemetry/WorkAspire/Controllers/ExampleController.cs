namespace WorkAspire.Controllers;

using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("[controller]/[action]")]
public class TestController : ControllerBase
{
    private static readonly HttpClient HttpClient = new();

    private readonly ILogger<TestController> log;

    private readonly ApiInstrument instrument;

    public TestController(ILogger<TestController> log, ApiInstrument instrument)
    {
        this.log = log;
        this.instrument = instrument;
    }

    [HttpGet]
    public async ValueTask<IActionResult> Execute()
    {
        using var activity = instrument.ActivitySource.StartActivity("Test");

        log.LogInformation("Execute.");

        try
        {
            var res = await HttpClient.GetStringAsync("http://google.com");

            activity?.SetTag("api.result", "OK");

            return Ok(res.Length);
        }
        catch (Exception)
        {
            activity?.SetTag("api.result", "NG");
            throw;
        }
    }
}

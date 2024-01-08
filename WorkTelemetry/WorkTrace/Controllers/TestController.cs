namespace WorkTrace.Controllers;

using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("[controller]/[action]")]
public class TestController : ControllerBase
{
    private static readonly HttpClient HttpClient = new();

    private readonly ApiInstrument instrument;

    public TestController(ApiInstrument instrument)
    {
        this.instrument = instrument;
    }

    [HttpGet]
    public async ValueTask<IActionResult> Execute()
    {
        using var activity = instrument.ActivitySource.StartActivity("Test");

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

namespace WorkLogHttpClient.Controllers;

using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("[controller]/[action]")]
public class TestController : ControllerBase
{
    [HttpGet]
    public async ValueTask<IActionResult> Execute([FromServices] IHttpClientFactory httpClientFactory)
    {
        using var client = httpClientFactory.CreateClient("Ipify");

        var address = await client.GetStringAsync(string.Empty).ConfigureAwait(false);

        return Ok(address);
    }
}

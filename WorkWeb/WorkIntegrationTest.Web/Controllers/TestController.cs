namespace WorkIntegrationTest.Web.Controllers;

using Microsoft.AspNetCore.Mvc;

public class Request
{
    public string Value { get; set; } = default!;
}

public class Response
{
    public string Value { get; set; } = default!;
}

[ApiController]
[Route("[controller]/[action]")]
public class TestController : ControllerBase
{
    [HttpPost]
    public IActionResult Execute([FromBody] Request request)
    {
        return Ok(new Response { Value = request.Value });
    }
}

namespace WorkCompress.Controllers;

using Microsoft.AspNetCore.Mvc;

public class PingRequest
{
    public string Message { get; set; } = default!;
}

public class PingResponse
{
    public string Message { get; set; } = default!;
}


[ApiController]
[Route("[controller]/[action]")]
public class TestController : ControllerBase
{
    [HttpPost]
    public IActionResult Ping([FromBody] PingRequest request)
    {
        return Ok(new PingResponse { Message = request.Message });
    }
}

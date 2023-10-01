namespace WorkWebLogContext.Controllers;

using Microsoft.AspNetCore.Mvc;

public class Form
{
    public string Data { get; set; } = default!;
}

[ApiController]
[Route("[controller]/[action]")]
public class TestController : ControllerBase
{
    private readonly ILogger<TestController> logger;

    public TestController(ILogger<TestController> logger)
    {
        this.logger = logger;
    }

    [HttpPost]
    [LogContext]
    public IActionResult Level1([FromBody] Form form)
    {
        logger.LogInformation("Level1. data=[{Data}]", form.Data);

        return Ok();
    }

    [HttpPost]
    [LogContext]
    public IActionResult Level2([FromBody] Form form)
    {
        logger.LogWarning("Level2. data=[{Data}]", form.Data);

        return Ok();
    }

    [HttpPost]
    [LogContext]
    public IActionResult Level3([FromBody] Form form)
    {
        logger.LogWarning("Level3. data=[{Data}]", form.Data);

        throw new Exception();
    }
}

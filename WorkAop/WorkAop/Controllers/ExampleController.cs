namespace WorkAop.Controllers;

using Microsoft.AspNetCore.Mvc;

using WorkAop.Services;

[ApiController]
[Route("[controller]/[action]")]
public class ExampleController : ControllerBase
{
    private readonly ITestService testService;

    public ExampleController(ITestService testService)
    {
        this.testService = testService;
    }

    [HttpGet]
    public IActionResult Execute()
    {
        return Ok(testService.Calc(1, 2));
    }
}

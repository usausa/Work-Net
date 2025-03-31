using Microsoft.AspNetCore.Mvc;

namespace WorkPlugin.Server.Controllers;

[ApiController]
[Route("[controller]/[action]")]
public class PluginController : ControllerBase
{
    [HttpGet]
    public IActionResult Test()
    {
        return Ok();
    }
}

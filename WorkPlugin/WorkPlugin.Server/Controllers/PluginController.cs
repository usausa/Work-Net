namespace WorkPlugin.Server.Controllers;

using Microsoft.AspNetCore.Mvc;

using WorkPlugin.Abstraction;

[ApiController]
[Route("[controller]/[action]")]
public class PluginController : ControllerBase
{
    private readonly List<IPlugin> plugins;

    public PluginController(IEnumerable<IPlugin> plugins)
    {
        this.plugins = plugins.ToList();
    }

    [HttpGet]
    public IActionResult Test()
    {
        return Ok(plugins.Select(static x => x.GetMessage()));
    }
}

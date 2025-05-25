using Microsoft.AspNetCore.Mvc;

namespace WorkCache.Controllers;

[ApiController]
[Route("[controller]/[action]")]
public class CacheController : ControllerBase
{
    [HttpGet("{id}")]
    public IActionResult Get(string id)
    {
        // TODO
        return Ok(id);
    }
}

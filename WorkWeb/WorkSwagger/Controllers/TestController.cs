#pragma warning disable IDE0060
namespace WorkSwagger.Controllers;

using Microsoft.AspNetCore.Mvc;

public class ListResponseEntry
{
    public int Id { get; set; }

    public string Name { get; set; } = default!;
}


public class ListResponse
{
    public ListResponseEntry[] Entry { get; set; } = default!;
}

public class UpdateRequest
{
    public int Id { get; set; }

    public string Name { get; set; } = default!;
}

[ApiController]
[Route("[controller]/[action]")]
public class TestController : ControllerBase
{
    [HttpGet]
    public IActionResult List()
    {
        return Ok(new ListResponse());
    }

    [HttpPost]
    public IActionResult Update([FromBody] UpdateRequest request)
    {
        return Ok();
    }
}

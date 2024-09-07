namespace WorkBindingQuery.Controllers;

using Microsoft.AspNetCore.Mvc;

public class Query
{
    // TODO
}

[ApiController]
[Route("[controller]/[action]")]
public class TestController : ControllerBase
{
    [HttpGet]
    public IActionResult Search(Query query)
    {
        return Ok(query);
    }
}

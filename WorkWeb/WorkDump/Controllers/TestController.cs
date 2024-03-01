namespace WorkDump.Controllers;

using Microsoft.AspNetCore.Mvc;

public class Request
{
    public string Id { get; set; } = default!;

    public int Value { get; set; }

    public class Entry
    {
        public string Name { get; set; } = default!;

        public int Value { get; set; }
    }

    public Entry[] Entries { get; set; } = default!;
}

public class Response
{
    public string Id { get; set; } = default!;

    public int Value { get; set; }

    public class Entry
    {
        public string Name { get; set; } = default!;

        public int Value { get; set; }
    }

    public Entry[] Entries { get; set; } = default!;
}

[ApiController]
[Route("[controller]/[action]")]
public class TestController : ControllerBase
{
    [HttpPost]
    public IActionResult Execute([FromBody] Request request)
    {
        return Ok(new Response
        {
            Id = request.Id,
            Value = request.Value,
            Entries = Enumerable.Range(1, 20).Select(x => new Response.Entry { Name = $"Name-{x}", Value = x }).ToArray()
        });
    }
}

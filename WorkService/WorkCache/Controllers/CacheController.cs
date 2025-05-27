using System.Diagnostics;

namespace WorkCache.Controllers;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;

public class GetResponse
{
    public string Body { get; set; } = default!;
}

public class PostRequest
{
    public string Body { get; set; } = default!;
}

[ApiController]
[Route("[controller]/[action]")]
public class CacheController : ControllerBase
{
    private readonly IDistributedCache cache;

    public CacheController(IDistributedCache cache)
    {
        this.cache = cache;
    }

    [HttpGet("{id}")]
    public async ValueTask<IActionResult> Get(string id)
    {
        var result = await cache.GetStringAsync($"data:{id}");
        if (result is null)
        {
            return NotFound();
        }

        return Ok(new GetResponse
        {
            Body = result
        });
    }

    [HttpPost("{id}")]
    public async ValueTask<IActionResult> Post(string id, [FromBody] PostRequest request)
    {
        await cache.SetStringAsync($"data:{id}", request.Body);

        return Ok();
    }

    [HttpPost("{id}")]
    public async ValueTask<IActionResult> Delete(string id)
    {
        await cache.RemoveAsync($"data:{id}");

        return Ok();
    }

    [HttpPost]
    public async ValueTask<IActionResult> Benchmark1()
    {
        var watch = Stopwatch.StartNew();

        for (var i = 0; i < 10000; i++)
        {
            await cache.GetStringAsync($"bench:{i:D8}");
            await cache.SetStringAsync($"bench:{i:D8}", "data");
        }

        return Ok(watch.ElapsedMilliseconds);
    }

    [HttpPost]
    public async ValueTask<IActionResult> Benchmark2()
    {
        var watch = Stopwatch.StartNew();

        for (var i = 0; i < 10000; i++)
        {
            await cache.GetStringAsync($"bench:{i:D8}");
        }

        return Ok(watch.ElapsedMilliseconds);
    }
}

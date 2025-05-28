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
    public IActionResult Benchmark1()
    {
        var watch = Stopwatch.StartNew();

        const int n = 10000;
        var tasks = new Task[10];
        for (var no = 0; no < tasks.Length; no++)
        {
            var start = no * n;
            // ReSharper disable once AsyncVoidLambda
            tasks[no] = Task.Run(async () =>
            {
                for (var i = start; i < start + n; i++)
                {
                    await cache.GetStringAsync($"bench:{i:D8}");
                    await cache.SetStringAsync($"bench:{i:D8}", "data");
                }
            });
        }

        Task.WaitAll(tasks);

        return Ok((double)n * tasks.Length / watch.ElapsedMilliseconds * 1000);
    }

    [HttpPost]
    public IActionResult Benchmark2()
    {
        var watch = Stopwatch.StartNew();

        const int n = 10000;
        var tasks = new Task[10];
        for (var no = 0; no < tasks.Length; no++)
        {
            var start = no * n;
            // ReSharper disable once AsyncVoidLambda
            tasks[no] = Task.Run(async () =>
            {
                for (var i = start; i < start + n; i++)
                {
                    await cache.GetStringAsync($"bench:{i:D8}");
                }
            });
        }

        Task.WaitAll(tasks);

        return Ok((double)n * tasks.Length / watch.ElapsedMilliseconds * 1000);
    }
}

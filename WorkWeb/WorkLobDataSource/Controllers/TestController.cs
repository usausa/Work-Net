namespace WorkLobDataSource.Controllers;

using Microsoft.AspNetCore.Mvc;

using Smart.Data;
using Smart.Data.Mapper;

[ApiController]
[Route("[controller]/[action]")]
public class TestController : ControllerBase
{
    [HttpGet]
    public async ValueTask<IActionResult> Query([FromServices] IDbProvider dbProvider)
    {
        await using var con = dbProvider.CreateConnection();
        var count = await con.ExecuteScalarAsync<int>("select count(*) from data");
        return Ok(count);
    }
}

namespace WorkTraceOracle.Controllers;

using Microsoft.AspNetCore.Mvc;

using Oracle.ManagedDataAccess.Client;

using Smart.Data.Mapper;

[ApiController]
[Route("[controller]/[action]")]
public class TestController : ControllerBase
{
    private readonly ApiInstrument instrument;

    public TestController(ApiInstrument instrument)
    {
        this.instrument = instrument;
    }

    [HttpGet]
    public async ValueTask<IActionResult> Execute()
    {
        using var activity = instrument.ActivitySource.StartActivity();

        await using var con = new OracleConnection("Data Source=oracle-db:1521/ORCLCDB;User Id=test;password=test");

        var count = await con.ExecuteScalarAsync<int>("select count(*) from data");

        return Ok(count);
    }
}

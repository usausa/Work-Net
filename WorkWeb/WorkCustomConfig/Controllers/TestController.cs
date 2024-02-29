namespace WorkCustomConfig.Controllers;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using WorkCustomConfig.Configuration;

[ApiController]
[Route("[controller]/[action]")]
public class TestController : ControllerBase
{
    private readonly SubSetting setting;

    private readonly IConfigurationOperator configurationOperator;

    public TestController(IOptionsSnapshot<SubSetting> setting, IConfigurationOperator configurationOperator)
    {
        this.setting = setting.Value;
        this.configurationOperator = configurationOperator;
    }

    [HttpGet]
    public IActionResult Get()
    {
        return Ok(setting);
    }

    [HttpPost]
    public IActionResult Set(int value)
    {
        configurationOperator.Update("Sub:Key2", value);
        return Ok();
    }
}

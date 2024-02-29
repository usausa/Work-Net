namespace WorkCustomConfig.Controllers;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.FeatureManagement;

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

    [HttpGet]
    public async ValueTask<IActionResult> Feature([FromServices] IFeatureManager featureManager)
    {
        return Ok(await featureManager.IsEnabledAsync("Custom"));
    }

    [HttpPost]
    public IActionResult Set(int value)
    {
        configurationOperator.Update("Sub:Key2", value);
        return Ok();
    }


    [HttpPost]
    public IActionResult Custom(int value)
    {
        configurationOperator.Update("FeatureManagement:Custom", value != 0);
        return Ok();
    }

    [HttpPost]
    public IActionResult Reload([FromServices] IConfiguration configuration)
    {
        ((IConfigurationRoot)configuration).Reload();
        return Ok(setting);
    }
}

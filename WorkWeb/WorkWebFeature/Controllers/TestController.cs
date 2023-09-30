namespace WorkWebFeature.Controllers;

using System.Diagnostics;

using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement;
using Microsoft.FeatureManagement.Mvc;

[ApiController]
[Route("[controller]/[action]")]
public class TestController : ControllerBase
{
    [HttpGet]
    public async ValueTask<IActionResult> Execute([FromServices] IFeatureManager featureManager)
    {
        Debug.WriteLine(await featureManager.IsEnabledAsync(FeatureFlags.CustomOption));

        return Ok();
    }

    [HttpGet]
    [FeatureGate(FeatureFlags.ExtendOption)]
    public IActionResult Extend()
    {
        return Ok();
    }

    [HttpGet]
    public async ValueTask<IActionResult> Percentage([FromServices] IFeatureManager featureManager)
    {
        Debug.WriteLine(await featureManager.IsEnabledAsync(FeatureFlags.PercentageOption));

        return Ok();
    }

    [HttpGet]
    public async ValueTask<IActionResult> Time([FromServices] IFeatureManager featureManager)
    {
        Debug.WriteLine(await featureManager.IsEnabledAsync(FeatureFlags.TimeOption));

        return Ok();
    }
}

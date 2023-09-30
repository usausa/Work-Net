namespace WorkWebFeature.Controllers;

using System.Diagnostics;

using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement;

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
}

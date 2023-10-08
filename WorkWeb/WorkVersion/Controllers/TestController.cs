//namespace WorkVersion.Controllers;

//using Asp.Versioning;
//using Microsoft.AspNetCore.Mvc;

//[ApiController]
//[ApiVersion(1.0, Deprecated = true)]
//[ApiVersion(2.0)]
//[Route("api/v{version:apiVersion}/[controller]/[action]")]
//public class TestController : ControllerBase
//{
//    [HttpGet]
//    public IActionResult Execute()
//    {
//        return Ok("v1");
//    }

//    [HttpGet]
//    [ActionName(nameof(Execute))]
//    [MapToApiVersion(2.0)]
//    public IActionResult ExecuteV2()
//    {
//        return Ok("v2");
//    }
//}

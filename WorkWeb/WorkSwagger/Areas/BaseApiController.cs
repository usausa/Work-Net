namespace WorkSwagger.Areas;

using System.Net.Mime;

using Microsoft.AspNetCore.Mvc;

[Route("api/[area]/[controller]/[action]")]
[ApiController]
[Consumes(MediaTypeNames.Application.Json)]
[Produces(MediaTypeNames.Application.Json)]
public abstract class BaseApiController : ControllerBase
{
}

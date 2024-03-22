namespace WorkSwagger.Areas.Sample1;

using Microsoft.AspNetCore.Mvc;

using System.Net.Mime;

using WorkSwagger.Areas;

[Area("sample1")]
[Consumes(MediaTypeNames.Application.Json)]
[Produces(MediaTypeNames.Application.Json)]
public abstract class BaseSample1Controller : BaseApiController
{
}

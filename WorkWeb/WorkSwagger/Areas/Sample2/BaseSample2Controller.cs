namespace WorkSwagger.Areas.Sample2;

using Microsoft.AspNetCore.Mvc;

using System.Net.Mime;

using WorkSwagger.Areas;

[Area("sample2")]
[Consumes(MediaTypeNames.Application.Json)]
[Produces(MediaTypeNames.Application.Json)]
public abstract class BaseSample2Controller : BaseApiController
{
}

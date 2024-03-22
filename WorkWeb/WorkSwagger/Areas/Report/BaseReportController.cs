namespace WorkSwagger.Areas.Report;

using Microsoft.AspNetCore.Mvc;

using System.Net.Mime;

using WorkSwagger.Areas;

[Area("report")]
[Consumes(MediaTypeNames.Application.Json)]
[Produces(MediaTypeNames.Application.Pdf)]
public abstract class BaseReportController : BaseApiController
{
}

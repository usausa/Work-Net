#pragma warning disable IDE0060
namespace WorkSwagger.Areas.Report.Controllers;

using Microsoft.AspNetCore.Mvc;

using System.Net.Mime;

using WorkSwagger.Application.Authentication;
using WorkSwagger.Application.Swagger;
using WorkSwagger.Areas.Report;

[MySwaggerTag(Tags.Report)]
public class ReportController : BaseReportController
{
    [HttpGet]
    [MySwaggerResponse(StatusCodes.Status200OK, MediaTypeNames.Application.Pdf)]
    public IActionResult Data(
        Credential credential)
    {
        // TODO
        return Ok(new byte[100]);
    }
}

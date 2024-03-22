#pragma warning disable IDE0060
namespace WorkSwagger.Areas.Report.Controllers;

using Microsoft.AspNetCore.Mvc;

using WorkSwagger.Application.Authentication;
using WorkSwagger.Areas.Report;

public class ReportController : BaseReportController
{
    [HttpGet]
    public IActionResult Data(
        Credential credential)
    {
        // TODO
        return Ok(new byte[100]);
    }
}

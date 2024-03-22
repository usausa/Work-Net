#pragma warning disable IDE0060
namespace WorkSwagger.Areas.Sample2.Controllers;

using System.Net.Mime;

using Microsoft.AspNetCore.Mvc;

using Swashbuckle.AspNetCore.Annotations;

using WorkSwagger.Application.Authentication;

public class ListResponseEntry
{
    public int Id { get; set; }

    public string Name { get; set; } = default!;
}

public class ListResponse
{
    public ListResponseEntry[] Entry { get; set; } = default!;
}

public class TestController : BaseSample2Controller
{
    [SwaggerOperation(
        Summary = "ドキュメント一覧取得",
        Description = "サンプルです",
        OperationId = "SampleDocumentList")]
    [SwaggerResponse(StatusCodes.Status200OK, "処理成功", typeof(ListResponse), MediaTypeNames.Application.Json)]
    [SwaggerResponse(StatusCodes.Status404NotFound, "該当無し")]
    [HttpGet]
    public IActionResult List(
        Credential credential)
    {
        return Ok(new ListResponse());
    }
}

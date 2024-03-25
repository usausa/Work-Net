#pragma warning disable IDE0060
namespace WorkSwagger.Areas.Sample2.Controllers;

using Microsoft.AspNetCore.Mvc;

using WorkSwagger.Application.Authentication;
using WorkSwagger.Application.Swagger;

public class ListResponseEntry
{
    public int Id { get; set; }

    public string Name { get; set; } = default!;
}

public class ListResponse
{
    public ListResponseEntry[] Entry { get; set; } = default!;
}

[SwaggerTag(Tags.Data1)]
public class TestController : BaseSample2Controller
{
    [SwaggerOperation("ドキュメント一覧取得", "サンプルです")]
    [SwaggerResponse(StatusCodes.Status200OK, typeof(ListResponse))]
    [SwaggerResponse(StatusCodes.Status404NotFound)]
    [HttpGet]
    public IActionResult List(
        Credential credential)
    {
        return Ok(new ListResponse());
    }
}

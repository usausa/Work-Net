#pragma warning disable IDE0060
namespace WorkSwagger.Areas.Sample2.Controllers;

using System.Net.Mime;

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

[MySwaggerTag(Tags.Data1)]
public class TestController : BaseSample2Controller
{
    [MySwaggerOperation("ドキュメント一覧取得", "サンプルです")]
    [MySwaggerResponse(StatusCodes.Status200OK, typeof(ListResponse))]
    [MySwaggerResponse(StatusCodes.Status404NotFound)]
    [HttpGet]
    public IActionResult List(
        Credential credential)
    {
        return Ok(new ListResponse());
    }
}

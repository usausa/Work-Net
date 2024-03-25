#pragma warning disable IDE0060
namespace WorkSwagger.Areas.Sample1.Controllers;

using System.ComponentModel.DataAnnotations;

using Microsoft.AspNetCore.Mvc;

using WorkSwagger.Application.Authentication;
using WorkSwagger.Application.Swagger;

public class ListRequest
{
    public string? Name { get; set; }
}

public class ListResponseEntry
{
    public int Id { get; set; }

    public string Name { get; set; } = default!;
}

public class ListResponse
{
    public ListResponseEntry[] Entry { get; set; } = default!;
}

//[SwaggerSchema(Required = ["Description"])]
//[SwaggerSchema]
public class UpdateRequest
{
    //[SwaggerSchema("The product identifier", ReadOnly = true)]
    [Required]
    public int Id { get; set; }

    //[SwaggerSchema("The product description")]
    [Required]
    public string Description { get; set; } = default!;

    //[SwaggerSchema("The date it was created", Format = "date")]
    [Required]
    public DateTime DateTime { get; set; }
}

[MySwaggerTag(Tags.Data1)]
public class TestController : BaseSample1Controller
{
    [MySwaggerOperation("ドキュメント一覧取得", "サンプルです")]
    [MySwaggerResponse(StatusCodes.Status200OK, typeof(ListResponse))]
    [MySwaggerResponse(StatusCodes.Status404NotFound)]
    [HttpGet]
    public IActionResult List(
        Credential credential,
        [FromQuery] ListRequest request)
    {
        return Ok(new ListResponse());
    }

    [MySwaggerOperation("ドキュメント更新", "サンプルです")]
    [MySwaggerResponse(StatusCodes.Status200OK)]
    [HttpPost]
    public IActionResult Update(
        Credential credential,
        [FromBody] UpdateRequest request)
    {
        return Ok();
    }
}

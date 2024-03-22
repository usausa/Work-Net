#pragma warning disable IDE0060
namespace WorkSwagger.Areas.Sample1.Controllers;

using System.ComponentModel.DataAnnotations;

using Microsoft.AspNetCore.Mvc;

using Swashbuckle.AspNetCore.Annotations;

using WorkSwagger.Application.Authentication;

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
[SwaggerSchema]
public class UpdateRequest
{
    [SwaggerSchema("The product identifier", ReadOnly = true)]
    [Required]
    public int Id { get; set; }

    [SwaggerSchema("The product description")]
    [Required]
    public string Description { get; set; } = default!;

    [SwaggerSchema("The date it was created", Format = "date")]
    [Required]
    public DateTime DateTime { get; set; }
}

public class TestController : BaseSample1Controller
{
    [SwaggerOperation(Summary = "ドキュメント一覧取得", Description = "サンプルです")]
    [SwaggerResponse(StatusCodes.Status200OK, "処理成功", typeof(ListResponse))]
    [SwaggerResponse(StatusCodes.Status404NotFound, "該当無し")]
    [HttpGet]
    public IActionResult List(
        Credential credential,
        [FromQuery] ListRequest request)
    {
        return Ok(new ListResponse());
    }

    [SwaggerOperation(Summary = "ドキュメント更新", Description = "サンプルです")]
    [SwaggerResponse(StatusCodes.Status200OK, "処理成功")]
    [HttpPost]
    public IActionResult Update(
        Credential credential,
        [FromBody] UpdateRequest request)
    {
        return Ok();
    }
}

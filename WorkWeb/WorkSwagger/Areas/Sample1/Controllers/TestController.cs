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

public enum UpdateType
{
    Zero,
    One,
    Two
}

public class UpdateRequest
{
    [Range(1, 99999)]
    public int ItemCode { get; set; }

    [Required]
    [MinLength(5)]
    [MaxLength(10)]
    public string Description { get; set; } = default!;

    [SwaggerScheme(example: "2000-12-31")]
    public DateOnly Date { get; set; }

    public DateTime DateTime { get; set; }

    public UpdateType Type { get; set; }
}

[SwaggerTag(Tags.Data1)]
public class TestController : BaseSample1Controller
{
    [SwaggerOperation("ドキュメント一覧取得", "ドキュメント一覧取得の詳細")]
    [SwaggerResponse(StatusCodes.Status200OK, typeof(ListResponse))]
    [SwaggerResponse(StatusCodes.Status404NotFound)]
    [HttpGet]
    public IActionResult List(
        Credential credential,
        [FromQuery] ListRequest request)
    {
        return Ok(new ListResponse());
    }

    [SwaggerOperation("ドキュメント更新", "ドキュメント更新の詳細")]
    [SwaggerResponse(StatusCodes.Status200OK)]
    [HttpPost]
    public IActionResult Update(
        Credential credential,
        [FromBody] UpdateRequest request)
    {
        return Ok();
    }
}

#pragma warning disable IDE0060
namespace WorkSwagger.Areas.Sample1.Controllers;

using System.ComponentModel.DataAnnotations;
using System.Net.Mime;

using Microsoft.AspNetCore.Mvc;

using Swashbuckle.AspNetCore.Annotations;

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

[ApiController]
[Consumes(MediaTypeNames.Application.Json)]
[Produces(MediaTypeNames.Application.Json)]
[Route("[controller]/[action]")]
public class TestController : ControllerBase
{
    [SwaggerOperation(
        Summary = "ドキュメント一覧取得",
        Description = "サンプルです",
        OperationId = "SampleDocumentList")]
    [SwaggerResponse(StatusCodes.Status200OK, "処理成功", typeof(ListResponse), MediaTypeNames.Application.Json)]
    [SwaggerResponse(StatusCodes.Status404NotFound, "該当無し")]
    //[ProducesResponseType<ListResponse>(StatusCodes.Status200OK)]
    //[ProducesResponseType(StatusCodes.Status404NotFound)]
    [HttpGet]
    public IActionResult List()
    {
        return Ok(new ListResponse());
    }

    [SwaggerOperation(
        Summary = "ドキュメント更新",
        Description = "サンプルです",
        OperationId = "SampleDocumentUpdate")]
    [SwaggerResponse(200, "処理成功")]
    [HttpPost]
    public IActionResult Update(
        [FromBody][SwaggerRequestBody("The product payload", Required = true)] UpdateRequest request)
    {
        return Ok();
    }
}

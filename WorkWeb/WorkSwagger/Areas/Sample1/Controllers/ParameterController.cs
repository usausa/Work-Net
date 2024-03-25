#pragma warning disable IDE0060
namespace WorkSwagger.Areas.Sample1.Controllers;

using System.ComponentModel.DataAnnotations;

using Microsoft.AspNetCore.Mvc;

using WorkSwagger.Application.Authentication;
using WorkSwagger.Application.Swagger;

public class ParameterUpdateRequest
{
    public int IntParam { get; set; }

    public int? NullableIntParam { get; set; }

    public bool BoolParam { get; set; }

    public bool? NullableBoolParam { get; set; }

    public DateTime DateTimeParam { get; set; }

    public DateTime? NullableDateTimeParam { get; set; }

    public string StringParam { get; set; } = default!;

    public string? NullableStringParam { get; set; }

    [Required]
    public int RequiredIntParam { get; set; }

    [Required]
    public int? RequiredNullableIntParam { get; set; }

    [Required]
    public string RequiredStringParam { get; set; } = default!;
}

[MySwaggerTag(Tags.Misc)]
public class ParameterController : BaseSample1Controller
{
    [HttpPost]
    [MySwaggerResponse(StatusCodes.Status200OK)]
    public IActionResult Update(
        Credential credential,
        [FromBody] ParameterUpdateRequest request)
    {
        return Ok();
    }
}

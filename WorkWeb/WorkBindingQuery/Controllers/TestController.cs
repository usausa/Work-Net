namespace WorkBindingQuery.Controllers;

using System.Collections.Frozen;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

[ApiController]
[Route("[controller]/[action]")]
public class TestController : ControllerBase
{
    [HttpGet]
    public IActionResult Search(
        [FromQuery][PageSize(10)][OrderBy("Id", "Name")] QueryParameter query)
    {
        return Ok(query);
    }
}

public class QueryParameter
{
    public int Page { get; set; }

    public int PageSize { get; set; }

    public string? OrderBy { get; set; }

    //public string? Filter { get; set; }
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public class PageSizeAttribute : Attribute
{
    public int Size { get; }

    public PageSizeAttribute(int size)
    {
        Size = size;
    }
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public class OrderByAttribute : Attribute
{
    public string[] Values { get; }

    public OrderByAttribute(params string[] values)
    {
        Values = values;
    }
}

public sealed class QueryParameterModelBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        if (context.Metadata.ModelType == typeof(QueryParameter))
        {
            var attributes = ((DefaultModelMetadata)context.Metadata).Attributes.Attributes;
            var pageSize = attributes.OfType<PageSizeAttribute>().FirstOrDefault();
            var orderBy = attributes.OfType<OrderByAttribute>().FirstOrDefault();
            return new QueryParameterModelBinder(pageSize?.Size ?? 10, orderBy?.Values ?? []);
        }

        return null;
    }
}

public sealed class QueryParameterModelBinder : IModelBinder
{
    private readonly int pageSize;

    private readonly FrozenSet<string> orders;

    public QueryParameterModelBinder(int pageSize, string[] orders)
    {
        this.pageSize = pageSize;
        this.orders = new HashSet<string>(orders).ToFrozenSet(StringComparer.OrdinalIgnoreCase);
    }

    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        var parameter = new QueryParameter { PageSize = pageSize };

        if (bindingContext.HttpContext.Request.Query.TryGetValue(nameof(QueryParameter.Page), out var result) &&
            Int32.TryParse(result, out var page))
        {
            parameter.Page = page;
        }

        if (bindingContext.HttpContext.Request.Query.TryGetValue(nameof(QueryParameter.OrderBy), out result) &&
            orders.TryGetValue(result!, out var setValue))
        {
            parameter.OrderBy = setValue;
        }

        bindingContext.Result = ModelBindingResult.Success(parameter);

        return Task.CompletedTask;
    }
}

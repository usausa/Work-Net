namespace Smart.AspNetCore.Exceptions
{
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Filters;

    public sealed class ExceptionStatusFilter : IExceptionFilter
    {
        public void OnException(ExceptionContext context)
        {
            if (context.Exception is HttpStatusException ex)
            {
                context.Result = new StatusCodeResult(ex.StatusCode);
                context.ExceptionHandled = true;
            }
        }
    }
}

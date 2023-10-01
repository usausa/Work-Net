namespace WorkWebLogContext;

using Serilog.Context;

using Microsoft.AspNetCore.Mvc.Filters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class LogContextAttribute : Attribute, IFilterFactory
{
    public bool IsReusable => true;

    public IFilterMetadata CreateInstance(IServiceProvider serviceProvider) =>
        serviceProvider.GetRequiredService<LogContextFilter>();

    public sealed class LogContextFilter : IAsyncActionFilter
    {
        private readonly ILogger<LogContextFilter> logger;

        public LogContextFilter(ILogger<LogContextFilter> logger)
        {
            this.logger = logger;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            logger.LogInformation("Context start.");

            using var _ = LogContext.PushProperty("Test", DateTime.Now.ToString("HH:mm:ss"));

            await next();

            logger.LogInformation("Context end.");
        }
    }
}

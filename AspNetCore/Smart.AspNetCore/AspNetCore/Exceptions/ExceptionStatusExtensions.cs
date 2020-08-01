namespace Smart.AspNetCore.Exceptions
{
    using Microsoft.AspNetCore.Mvc.Filters;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;

    public static class ExceptionStatusExtensions
    {
        public static IServiceCollection AddExceptionStatus(this IServiceCollection services)
        {
            services.TryAddSingleton<ExceptionStatusFilter>();

            return services;
        }

        public static IFilterMetadata AddExceptionStatus(this FilterCollection filters)
        {
            return filters.AddService<ExceptionStatusFilter>();
        }
    }
}

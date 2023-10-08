namespace WorkStartup.Controllers;

using System.Diagnostics;

public class ApplicationStartupFilter : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        return builder =>
        {
            //builder.ApplicationServices.GetService()

            Debug.WriteLine("* Startup");

            next(builder);
        };
    }
}

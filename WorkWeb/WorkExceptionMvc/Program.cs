using WorkExceptionMvc;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddEndpointsApiExplorer();

// [MEMO] Custom problem
builder.Services.AddProblemDetails(static options =>
{
    options.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Extensions.Add("nodeId", Environment.MachineName);
    };
});

var app = builder.Build();

app.UseWhen(
    static c => c.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase),
    static b => b.UseExceptionHandler(),
    b =>
    {
        //if (!app.Environment.IsDevelopment())
        {
            b.UseExceptionHandler("/Home/Error");
        }
        b.UseStatusCodePagesWithReExecute("/error/{0}");
    });

//app.UseWhen(
//    static c => c.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase),
//    static b => b.UseExceptionHandler());
//app.UseWhen(
//    static c => !c.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase),
//    b =>
//    {
//        //if (!app.Environment.IsDevelopment())
//        {
//            b.UseExceptionHandler("/Home/Error");
//        }
//        b.UseStatusCodePagesWithReExecute("/error/{0}");
//    });

// Configure the HTTP request pipeline.
//if (!app.Environment.IsDevelopment())
//{
//    app.UseExceptionHandler("/Home/Error");
//}

// Default
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

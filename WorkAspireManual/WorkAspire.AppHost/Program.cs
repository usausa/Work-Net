var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.WorkAspire_ApiService>("workaspire-apiservice");

builder.Build().Run();

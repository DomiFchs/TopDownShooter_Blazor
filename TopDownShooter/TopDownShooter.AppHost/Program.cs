var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder.AddProject<Projects.TopDownShooter_ApiService>("apiservice");

builder.AddProject<Projects.View>("webfrontend").WithReference(apiService);

builder.Build().Run();
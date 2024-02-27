/*var builder = DistributedApplication.CreateBuilder(args);


var shooterdb = builder.AddSqlServerContainer("shootersql", "dominik05!")
    .WithVolumeMount("shooterdata", "/var/opt/mssql/data", VolumeMountType.Named)
    .AddDatabase("shooterdb");


var apiService = builder.AddProject<Projects.TopDownShooter_ApiService>("apiservice")
    .WithReference(shooterdb);

builder.AddProject<Projects.View>("webfrontend").WithReference(apiService);

builder.Build().Run();*/
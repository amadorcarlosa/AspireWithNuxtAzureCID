var builder = DistributedApplication.CreateBuilder(args);

// For Azure deployment (add when ready)
builder.AddAzureContainerAppEnvironment("aca-env");


var webApi = builder.AddProject<Projects.WebApi>("webapi")
    .WithHttpHealthCheck("/health");  // recommended

var webApp = builder.AddJavaScriptApp("webapp", "../WebApp")
    .WithPnpm()
    .WithHttpEndpoint(port: 4000, isProxied: false)
    .WithExternalHttpEndpoints()
    .WithReference(webApi)
    .WaitFor(webApi)
    .WithEnvironment("API_BASE_URL", webApi.GetEndpoint("https"));

builder.Build().Run();
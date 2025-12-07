using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var webApi = builder.AddProject<Projects.WebApi>("WebApi");


if(builder.Environment.IsDevelopment()){
var webApp = builder.AddJavaScriptApp("WebApp", "../WebApp")
    .WithNpm()
    .WithRunScript("dev")
    .WithHttpEndpoint(port: 4000, isProxied: false)
    .WithExternalHttpEndpoints()
    .WithReference(webApi)
    .WaitFor(webApi)
    .WithEnvironment("API_BASE_URL", webApi.GetEndpoint("https"));
}
if(builder.Environment.IsProduction()){
var webApp = builder.AddJavaScriptApp("webapp", "../WebApp")
    .WithNpm()
    .WithExternalHttpEndpoints()
    .WithReference(webApi)
    .WaitFor(webApi)
    .WithEnvironment("API_BASE_URL", webApi.GetEndpoint("https"));

}

builder.Build().Run();
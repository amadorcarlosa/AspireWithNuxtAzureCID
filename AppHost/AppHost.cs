using Microsoft.Extensions.Hosting;

// Create the Aspire distributed application builder
var builder = DistributedApplication.CreateBuilder(args);

// Add the WebApi project to the application
var webApi = builder.AddProject<Projects.WebApi>("web-api");

// Configure the WebApp differently based on the environment
if(builder.Environment.IsDevelopment()){
    // Development: Run the Nuxt app with hot reload on port 4000
    var webApp = builder.AddJavaScriptApp("web-app", "../WebApp")
        .WithNpm()
        .WithRunScript("dev")
        .WithHttpEndpoint(port: 4000, isProxied: false)
        .WithExternalHttpEndpoints()
        .WithReference(webApi)
        .WaitFor(webApi)
        .WithEnvironment("API_BASE_URL", webApi.GetEndpoint("https"));
}
if(builder.Environment.IsProduction()){
    // Production: Run the Nuxt app in production mode
    var webApp = builder.AddJavaScriptApp("web-app", "../WebApp")
        .WithNpm()
        .WithExternalHttpEndpoints()
        .WithReference(webApi)
        .WaitFor(webApi)
        .WithEnvironment("API_BASE_URL", webApi.GetEndpoint("https"));

}

// Build and run the distributed application
builder.Build().Run();
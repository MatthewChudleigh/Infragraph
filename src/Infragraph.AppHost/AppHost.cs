var builder = DistributedApplication.CreateBuilder(args);

var server = builder.AddProject<Projects.Infragraph_Server>("server")
    .WithHttpHealthCheck("/health")
    .WithExternalHttpEndpoints()
    .WithEnvironment("ASPNETCORE_HTTPS_PORTS", "");

var webfrontend = builder.AddViteApp("webfrontend", "../Infragraph.Web")
    .WithReference(server)
    .WaitFor(server);

server.PublishWithContainerFiles(webfrontend, "wwwroot");

builder.Build().Run();

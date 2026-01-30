using Infragraph.Server.Configuration;
using Infragraph.Server.Endpoints;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddProblemDetails();

// Add Infragraph services
builder.Services.AddInfragraphServices();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Map API endpoints
var api = app.MapGroup("/api");
api.MapDiagramEndpoints();
api.MapResourceEndpoints();
api.MapExportEndpoints();

app.MapDefaultEndpoints();

app.UseFileServer();

app.Run();

using EnterpriseRag.Infrastructure;
using EnterpriseRag.Ingestion;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHostedService<IngestionService>();

var host = builder.Build();
await host.RunAsync();

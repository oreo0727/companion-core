using Companion.Infrastructure;
using Companion.Worker.Services;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHostedService<AgentRunWorker>();
builder.Services.AddHostedService<ReminderWorker>();
builder.Services.AddHostedService<ConnectorSyncWorker>();

var host = builder.Build();
await host.Services.InitializeDatabaseAsync();
await host.RunAsync();

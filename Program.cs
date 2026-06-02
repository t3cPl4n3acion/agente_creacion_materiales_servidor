using AgentDataApi.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAgentDataApiServices(builder.Configuration);

var app = builder.Build();

app.UseAgentDataApiPipeline();

app.Run();

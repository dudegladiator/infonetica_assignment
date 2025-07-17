

using Infonetica.src.Endpoints;
using Infonetica.src.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<WorkflowService>();

var app = builder.Build();

// wire up all our workflow-definition routes
app.MapWorkflowEndpoints();
app.MapGet("/", () => "Welcome to the Infonetica Workflow API!");

app.Run();

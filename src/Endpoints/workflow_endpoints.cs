// src/Endpoints/WorkflowEndpoints.cs
using System.Text.Json;
using Infonetica.src.Models;
using Infonetica.src.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Infonetica.src.Endpoints
{
    public static class WorkflowEndpoints
    {
        public static void MapWorkflowEndpoints(this WebApplication app)
        {
            // 1. Create a new workflow definition
            app.MapPost("/definitions", async (HttpContext http, WorkflowService svc) =>
            {
                WorkflowDefinition def;
                try
                {
                    def = await http.Request.ReadFromJsonAsync<WorkflowDefinition>() 
                          ?? throw new JsonException("Request body was empty");
                }
                catch (JsonException ex)
                {
                    return Results.BadRequest(new { error = $"Invalid JSON payload: {ex.Message}" });
                }

                // Basic payload validation
                if (string.IsNullOrWhiteSpace(def.Id))
                    return Results.BadRequest(new { error = "Field 'Id' is required." });
                if (string.IsNullOrWhiteSpace(def.Description))
                    return Results.BadRequest(new { error = "Field 'Description' is required." });
                if (def.States == null || !def.States.Any())
                    return Results.BadRequest(new { error = "At least one State is required." });
                if (def.Actions == null || !def.Actions.Any())
                    return Results.BadRequest(new { error = "At least one Action is required." });

                // Delegate to service for deeper validation
                var (ok, validationError) = svc.CreateDefinition(def);
                if (!ok)
                    return Results.BadRequest(new { error = validationError });

                return Results.Created($"/definitions/{def.Id}", def);
            });

            // 2. List all definitions
            app.MapGet("/definitions", (WorkflowService svc) =>
                Results.Ok(svc.GetAllDefinitions())
            );

            // 3. Get a single definition by Id
            app.MapGet("/definitions/{id}", (string id, WorkflowService svc) =>
            {
                if (string.IsNullOrWhiteSpace(id))
                    return Results.BadRequest(new { error = "Parameter 'id' cannot be empty." });

                var def = svc.GetDefinition(id);
                return def is not null
                    ? Results.Ok(def)
                    : Results.NotFound(new { error = $"Definition '{id}' not found." });
            });

            // 4. Start a new instance with a description
            app.MapPost("/definitions/{id}/instances", async (HttpContext http, string id, WorkflowService svc) =>
            {
                // 1) Require JSON content type
                if (string.IsNullOrWhiteSpace(http.Request.ContentType) ||
                    !http.Request.ContentType.Contains("application/json", StringComparison.OrdinalIgnoreCase))
                {
                    return Results.BadRequest(new { error = "Request must have Content-Type: application/json" });
                }

                // 2) Attempt to parse the body
                CreateInstanceRequest req;
                try
                {
                    req = await http.Request.ReadFromJsonAsync<CreateInstanceRequest>()
                        ?? throw new JsonException("Request body was empty");
                }
                catch (JsonException ex)
                {
                    return Results.BadRequest(new { error = $"Invalid JSON payload: {ex.Message}" });
                }
                catch (InvalidOperationException ex)
                {
                    // This catches the "unknown content type" error from System.Text.Json
                    return Results.BadRequest(new { error = ex.Message });
                }

                // 3) Validate the payload
                if (string.IsNullOrWhiteSpace(req.Description))
                    return Results.BadRequest(new { error = "Field 'Description' is required." });

                // 4) Delegate to service
                var result = svc.StartInstance(id, req.Description);
                if (!result.Success)
                    return Results.BadRequest(new { error = result.Error });

                var inst = result.Instance!;
                return Results.Created($"/instances/{inst.Id}", inst);
            });

            // 5. Execute an action on an instance
            app.MapPost("/instances/{instId}/actions/{actionId}", (string instId, string actionId, WorkflowService svc) =>
            {
                if (string.IsNullOrWhiteSpace(instId) || string.IsNullOrWhiteSpace(actionId))
                    return Results.BadRequest(new { error = "Both 'instId' and 'actionId' must be provided." });

                var (ok, error, inst) = svc.ExecuteAction(instId, actionId);
                if (!ok)
                    return Results.BadRequest(new { error });

                return Results.Ok(inst);
            });

            // 6. Inspect a single instance
            app.MapGet("/instances/{instId}", (string instId, WorkflowService svc) =>
            {
                if (string.IsNullOrWhiteSpace(instId))
                    return Results.BadRequest(new { error = "Parameter 'instId' cannot be empty." });

                var inst = svc.GetInstance(instId);
                return inst is not null
                    ? Results.Ok(inst)
                    : Results.NotFound(new { error = $"Instance '{instId}' not found." });
            });

            // 7. List all instances
            app.MapGet("/instances", (WorkflowService svc) =>
                Results.Ok(svc.GetAllInstances())
            );
        }
    }
}

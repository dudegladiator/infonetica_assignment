// src/Models/WorkflowDefinition.cs
namespace Infonetica.src.Models;

public record WorkflowDefinition
{
    public string Id            { get; init; } = null!;
    public string? Description  { get; init; }
    public DateTime CreatedAt   { get; init; } = DateTime.UtcNow;
    public List<State> States   { get; init; } = new();
    public List<ActionDefinition> Actions { get; init; } = new();
}

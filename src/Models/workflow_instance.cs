namespace Infonetica.src.Models;

public class WorkflowInstance
{
    public string Id            { get; init; } = Guid.NewGuid().ToString();
    public string DefinitionId  { get; init; } = null!;
    public string Description   { get; init; } = null!;            // ← new
    public string CurrentState  { get; set; } = null!;
    public List<HistoryEntry> History { get; init; } = new();
}

public record CreateInstanceRequest(
    string Description
);
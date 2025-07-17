namespace Infonetica.src.Models;

public record ActionDefinition(
    string Id,
    string Name,
    bool Enabled,
    List<string> FromStates,
    string ToState,
    string? Description
);
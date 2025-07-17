namespace Infonetica.src.Models;

public record State(
    string Id,
    string Name,
    bool IsInitial,
    bool IsFinal,
    bool Enabled,
    string? Description    // ← new
);

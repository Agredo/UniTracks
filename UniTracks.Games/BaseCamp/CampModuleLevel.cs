namespace UniTracks.Games.BaseCamp;

/// <summary>Level of a single camp module — the only state a module has.</summary>
public record CampModuleLevel
{
    public string ModuleId { get; init; } = string.Empty;

    public int Level { get; init; }
}

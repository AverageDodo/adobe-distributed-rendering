namespace DistributedRendering.AME.Shared.DTOs;

public interface IRenderFragment
{
    public Guid Guid { get; init; }
    public Guid RequestGuid { get; init; }
    public string ProjectPath { get; init; }
    public string PresetPath { get; init; }
    public string DestinationPath { get; init; }
    public int Index { get; init; }
    public long StartTimeInMilliseconds { get; init; }
    public int DurationInMilliseconds { get; init; }
}

public record RenderFragment : IRenderFragment
{
    public required Guid Guid { get; init; }
    public required Guid RequestGuid { get; init; }
    public required string ProjectPath { get; init; }
    public required string PresetPath { get; init; }
    public required string DestinationPath { get; init; }
    public required int Index { get; init; }
    public required long StartTimeInMilliseconds { get; init; }
    public required int DurationInMilliseconds { get; init; }
}
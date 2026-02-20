namespace DistributedRendering.AME.Shared.Enums;

[Flags]
public enum RenderStatus
{
    None = 0,
    Started = 1,
    Completed = 2,
    Failed = 4
}
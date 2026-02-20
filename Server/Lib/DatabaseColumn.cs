namespace DistributedRendering.AME.Server.Lib;

public readonly record struct DatabaseColumn
{
	public required string Name { get; init; }
	public required short Ordinal { get; init; }
}
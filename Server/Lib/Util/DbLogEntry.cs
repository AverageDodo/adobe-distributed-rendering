namespace DistributedRendering.AME.Server.Lib.Util;

public readonly record struct DbLogEntry(
	string TimeStamp,
	uint Level,
	string? Category,
	string? Message,
	string? Exception
);
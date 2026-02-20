using DistributedRendering.AME.Server.Lib.Interfaces;

namespace DistributedRendering.AME.Server.Lib.Models;

/// <summary>
///     Implementation of <see cref="IRenderRequest" /> representing a render request.
/// </summary>
internal sealed class RenderRequest : IRenderRequest
{
	/// <inheritdoc />
	public required Guid Guid { get; set; }

	/// <inheritdoc />
	public required DirectoryInfo RequestDirectory { get; set; }

	/// <inheritdoc />
	public required string ProjectPath { get; set; }

	/// <inheritdoc />
	public required string EncoderPresetPath { get; set; }

	/// <inheritdoc />
	public required string OutputPath { get; set; }

	/// <inheritdoc />
	public required TimeSpan TimelineDuration { get; set; }

	/// <inheritdoc />
	public DateTime CreatedAt { get; set; }

	/// <inheritdoc />
	public DateTime CompletedAt { get; set; }
}
namespace DistributedRendering.AME.Server.Lib.Interfaces;

/// <summary>
///     Represents a render request containing all necessary information for processing a rendering job.
/// </summary>
public interface IRenderRequest
{
    /// <summary>
    ///     Gets or sets the unique identifier for the render request.
    /// </summary>
    Guid Guid { get; set; }

    /// <summary>
    ///     Gets or sets the directory containing the request data.
    /// </summary>
    DirectoryInfo RequestDirectory { get; set; }

    /// <summary>
    ///     Gets or sets the path to the project file to be rendered.
    /// </summary>
    string ProjectPath { get; set; }

    /// <summary>
    ///     Gets or sets the path to the encoder preset file.
    /// </summary>
    string EncoderPresetPath { get; set; }

    /// <summary>
    ///     Gets or sets the output path for the rendered result.
    /// </summary>
    string OutputPath { get; set; }

    /// <summary>
    ///     Gets or sets the duration of the timeline in milliseconds.
    /// </summary>
    TimeSpan TimelineDuration { get; set; }

    /// <summary>
    ///     Gets or sets the creation timestamp of the render request.
    /// </summary>
    DateTime CreatedAt { get; set; }

    /// <summary>
    ///     Gets or sets the completion timestamp of the render request.
    /// </summary>
    DateTime CompletedAt { get; set; }
}
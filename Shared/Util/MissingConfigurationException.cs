namespace DistributedRendering.AME.Shared.Util;

/// <summary>
///     Represents an exception that is thrown when a required configuration setting is missing.
/// </summary>
/// <remarks>
///     Use this exception to indicate that an expected configuration value was not found, preventing the
///     application from proceeding. This exception is typically thrown during application startup or configuration loading
///     when a mandatory setting is absent.
/// </remarks>
public sealed class MissingConfigurationException : Exception
{
    public MissingConfigurationException(string message) : base(message) { }

    public MissingConfigurationException(Type missingSectionType) : base(
        $"The required configuration section '{missingSectionType.Name}' was not found."
    ) { }
}
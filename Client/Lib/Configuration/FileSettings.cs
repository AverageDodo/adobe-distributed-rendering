namespace DistributedRendering.AME.Client.Lib.Configuration;

public sealed record FileSettings
{
	public FileSettings(string appDataDirectoryName, string scriptTemplatePath, string scriptTemplateSlug)
	{
		AppDataDirectoryName = appDataDirectoryName;
		ScriptTemplatePath = scriptTemplatePath;
		ScriptTemplateSlug = scriptTemplateSlug;

		AppdataDirectory = new DirectoryInfo(
			Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
				AppDataDirectoryName
			)
		);

		if (!AppdataDirectory.Exists)
			AppdataDirectory.Create();

		TempDirectory = new DirectoryInfo(
			Path.Combine(AppdataDirectory.FullName, "temp")
		);

		if (!TempDirectory.Exists)
			TempDirectory.Create();
	}

	public required string AppDataDirectoryName { get; init; }
	public required string ScriptTemplatePath { get; init; }
	public required string ScriptTemplateSlug { get; init; }

	public DirectoryInfo AppdataDirectory { get; init; }

	public DirectoryInfo TempDirectory { get; init; }
}
using System.Text.Json;

using DistributedRendering.AME.Client.Lib.Configuration;
using DistributedRendering.AME.Client.Services.Interfaces;
using DistributedRendering.AME.Shared.DTOs;
using DistributedRendering.AME.Shared.Util;

namespace DistributedRendering.AME.Client.Services;

internal sealed class ScriptGenerationService(
	ILogger<ScriptGenerationService> logger,
	IConfigurationService configurationService
) : IScriptGenerationService
{
	private string ScriptTemplatePath => Path.Combine(
		configurationService.ProgramDataDirectory.FullName,
		configurationService.FileSettings.ScriptTemplatePath
	);

	public async Task<FileInfo?> GenerateRenderScriptAsync(IRenderFragment jobData, CancellationToken token = default)
	{
		if (token.IsCancellationRequested) return null;

		FileSettings fileSettings = configurationService.FileSettings;
		DirectoryInfo appdataDir = fileSettings.AppdataDirectory;

		if (!appdataDir.Exists)
		{
			appdataDir.Create();
			appdataDir.Refresh();

			if (!appdataDir.Exists)
				throw new DirectoryNotFoundException(
					$"The required appdata directory does not exist at the specified path '{appdataDir.FullName}'."
				);
		}

		string scriptFilePath = Path.Combine(
			configurationService.FileSettings.AppdataDirectory.FullName,
			$"{jobData.Guid}.js"
		);

		var script = new FileInfo(scriptFilePath);
		try
		{
			using StreamReader templateReader = File.OpenText(ScriptTemplatePath);
			string templateScriptContent = await templateReader.ReadToEndAsync(token);
			templateReader.Close();

			await using FileStream fs = script.Open(FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
			await using var writer = new StreamWriter(fs);

			// The media encoder adds the file extension to the output file automatically, so we strip it away
			RenderFragment data = (jobData as RenderFragment)! with
			{
				DestinationPath = jobData.DestinationPath.Split(".").First()
			};

			string generatedScriptContent = templateScriptContent.Replace(
				fileSettings.ScriptTemplateSlug,
				JsonSerializer.Serialize(data, JsonConfig.SerializerOptions)
			);

			await writer.WriteAsync(generatedScriptContent);
			await writer.FlushAsync(token);
			writer.Close();

			script = new FileInfo(scriptFilePath);
			logger.LogInformation(
				"Render script for job '{Guid}' generated at '{Path}'.",
				jobData.Guid,
				script.FullName
			);
		}
		catch (Exception e)
		{
			logger.LogError(e, "Exception caught while attempting to generate render script file.");

			script = null;
		}

		return script;
	}
}
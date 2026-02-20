using System.Diagnostics;
using System.Security.Cryptography;

using DistributedRendering.AME.Client.Services.Interfaces;
using DistributedRendering.AME.Shared.DTOs;
using DistributedRendering.AME.Shared.Enums;
using DistributedRendering.AME.Shared.Interfaces;
using DistributedRendering.AME.Shared.Util;

namespace DistributedRendering.AME.Client.Services;

internal sealed class MediaEncoderService(
	ILogger<MediaEncoderService> logger,
	IConfigurationService configurationService,
	IScriptGenerationService scriptGenerationService,
	IStateService stateService
) : IMediaEncoderService
{
	private static readonly string? MediaEncoderPath;
	private readonly string instanceId = RandomNumberGenerator.GetHexString(5);

	private readonly ProcessStartInfo mediaEncoderStartInfo = new()
	{
		RedirectStandardOutput = true,
		RedirectStandardError = true,
		FileName = MediaEncoderPath ?? configurationService.MediaEncoderSettings.MediaEncoderExecutablePath
	};

	private FileSystemWatcher? fileSystemWatcher;
	private IRenderFragment? fragment;

	private Process? mediaEncoderProcess;

	private Timer? processTimeoutTimer;
	private RenderReport? renderReport;
	private FileInfo? renderScript;
	private FileInfo? tempOutputFile;

	static MediaEncoderService()
	{
		string adobeInstallPath = Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
			"Adobe"
		);

		var adobeDirectory = new DirectoryInfo(adobeInstallPath);

		if (!adobeDirectory.Exists) return;

		List<FileInfo> mediaEncoderInstalls =
			adobeDirectory
				.EnumerateDirectories()
				.Where(dirInfo => dirInfo.Name.Contains("Adobe Media Encoder"))
				.SelectMany(dirInfo => dirInfo.GetFiles().Where(file => file.Name.Equals("Adobe Media Encoder.exe")))
				.OrderBy(file => file.Directory?.Name)
				.ToList();

		MediaEncoderPath = mediaEncoderInstalls.LastOrDefault()?.FullName;
	}

	public event EventHandler<IRenderReport> RenderCompleteEvent = (_, _) => { };

	public async Task<bool> StartRenderProcessAsync(IRenderFragment data, CancellationToken token = default)
	{
		logger.LogDebug("Media encoder service instance '{Id}' created.", instanceId);

		tempOutputFile = new FileInfo(
			Path.Combine(
				configurationService.FileSettings.TempDirectory.FullName,
				$"{RandomNumberGenerator.GetHexString(10, true)}.mp4"
			)
		);

		FileInfo? script = await scriptGenerationService.GenerateRenderScriptAsync(
			(RenderFragment)data with { DestinationPath = tempOutputFile.FullName },
			token
		);

		if (script is not { Exists: true })
			throw new ArgumentException(
				"The supplied render script does not exist.",
				new FileNotFoundException("The specified file does not exist.", script?.FullName)
			);

		fragment = data;
		renderScript = script;

		renderReport = new RenderReport((RenderFragment)fragment)
		{
			Guid = Guid.NewGuid(),
			FragmentReceived = DateTime.UtcNow
		};

		mediaEncoderStartInfo.ArgumentList.Clear();

		// Passing them together as one string does not work for some reason, 
		// probably because the whitespace is escaped
		string args = configurationService.MediaEncoderSettings.ProcessArguments;
		foreach (string arg in args.Split(" "))
			mediaEncoderStartInfo.ArgumentList.Add(arg);

		mediaEncoderStartInfo.ArgumentList.Add(script.FullName);
		mediaEncoderProcess = new Process
		{
			StartInfo = mediaEncoderStartInfo,
			EnableRaisingEvents = true
		};

		mediaEncoderProcess.Exited += OnProcessExit;

		fileSystemWatcher = new FileSystemWatcher
		{
			Path = tempOutputFile.DirectoryName!,
			Filter = "*",
			EnableRaisingEvents = true,
			NotifyFilter = NotifyFilters.FileName
		};

		fileSystemWatcher.Created += OnOutputFileCreated;

		if (!mediaEncoderProcess.Start())
		{
			renderReport.AddFlagToReport(RenderStatus.Failed);
			renderReport.AddOrAppendErrorMessage("Failed to start Media Encoder process.");

			// This task ensures that a render report is sent after the calling method has returned. 
			_ = Task.Run(
				async () =>
				{
					await Task.Delay(TimeSpan.FromSeconds(3), token);
					RenderCompleteEvent.Invoke(this, renderReport);
				},
				CancellationToken.None
			);

			return false;
		}

		renderReport.LoadingStarted = DateTime.UtcNow;
		stateService.SetStatus(ClientStatus.Loading, out _);

		processTimeoutTimer = new Timer(
			OnProcessTimeout,
			null,
			configurationService.MediaEncoderSettings.ProcessTimeoutInterval,
			Timeout.InfiniteTimeSpan
		);

		return true;
	}

	#region Event Callbacks

	private void OnProcessTimeout(object? state)
	{
		try
		{
			if (mediaEncoderProcess is null)
				return;

			logger.LogInformation(
				"Media Encoder process with ID '{Id}' exceeded the maximum lifetime and will be killed.",
				mediaEncoderProcess.Id
			);

			mediaEncoderProcess.Kill(true);
			renderReport.AddOrAppendErrorMessage("Render process exceeded maximum lifetime and was killed.");
		}
		catch (Exception e)
		{
			renderReport.AddFlagToReport(RenderStatus.Failed);
			renderReport.AddOrAppendErrorMessage($"Error occured: {e.Message}");

			logger.LogError(e, "Exception caught in 'OnProcessTimeout' callback.");
		}
		finally
		{
			RenderCompleteEvent.Invoke(this, renderReport!);
		}
	}

	private void OnProcessExit(object? sender, EventArgs args)
	{
		logger.LogInformation(
			"Media encoder process has exited with exit code '{Code}.'",
			mediaEncoderProcess?.ExitCode
		);

		try
		{
			renderScript?.Refresh();
			if (renderScript is { Exists: true })
				renderScript.Delete();

			tempOutputFile!.Refresh();
			if (!tempOutputFile.Exists)
			{
				renderReport.AddFlagToReport(RenderStatus.Failed);
				renderReport.AddOrAppendErrorMessage("No output file was found after Media Encoder Process exited.");
			}
			else
			{
				renderReport.AddFlagToReport(RenderStatus.Completed);
				renderReport!.ExportCompleted = DateTime.UtcNow;

				var finalOutputFile = new FileInfo(fragment!.DestinationPath);
				var tempFileOnNas = new FileInfo(
					Path.Combine(
						finalOutputFile.DirectoryName!,
						tempOutputFile.Name
					)
				);

				// Initiate transfer of output file to NAS
				FileTransferService.QueueFileTransfer(tempOutputFile, tempFileOnNas, finalOutputFile.Name);
			}
		}
		catch (Exception e)
		{
			logger.LogError(e, "Exception caught in process exited callback.");
		}
		finally
		{
			RenderCompleteEvent.Invoke(this, renderReport!);
		}
	}

	private void OnOutputFileCreated(object sender, FileSystemEventArgs e)
	{
		try
		{
			if (e.ChangeType != WatcherChangeTypes.Created || string.IsNullOrWhiteSpace(e.Name))
				return;

			var createdFile = new FileInfo(e.FullPath);

			if (renderReport is null || fragment is null)
			{
				logger.LogWarning(
					"FileSystemWatcher detected a new file in directory '{Dir}', but render report or render fragment are null.",
					createdFile.DirectoryName
				);

				if (sender is FileSystemWatcher watcher)
					watcher.Dispose();

				return;
			}

			// Once rendering is complete, AME will create a .tmp file which is used to copy to the final encoded output file
			if (createdFile.Extension.Equals(".tmp", StringComparison.InvariantCultureIgnoreCase))
			{
				if (renderReport.EncodingStarted == default)
				{
					renderReport.EncodingStarted = DateTime.UtcNow;

					logger.LogInformation(
						"Rendering for fragment '{Guid}' is complete. Beginning encoding...",
						fragment.Guid
					);
				}

				return;
			}

			// If the created file is not the output file but contains part of the output file name, 
			// then the encoding process has started since these files are temporary files used by 
			// the media encoder.
			bool isTempMediaFile = createdFile.Extension.Equals(".m4v", StringComparison.InvariantCultureIgnoreCase) ||
			                       createdFile.Extension.Equals(".aac", StringComparison.InvariantCultureIgnoreCase);

			if (isTempMediaFile)
				if (renderReport.RenderStarted == default)
				{
					logger.LogInformation("Temp file for fragment found. Marking render as started.");

					renderReport.AddFlagToReport(RenderStatus.Started);
					renderReport.RenderStarted = DateTime.UtcNow;
					stateService.SetStatus(ClientStatus.Rendering, out _);
				}
		}
		catch (Exception exception)
		{
			logger.LogError(exception, "Exception caught in 'FileSystemWatcher.Created' callback.");
		}
	}

	#endregion

	#region Dispose

	private bool disposedValue;

	private void Dispose(bool disposing)
	{
		if (disposedValue) return;

		if (disposing)
		{
			if (processTimeoutTimer is not null)
			{
				processTimeoutTimer.Dispose();
				processTimeoutTimer = null;
			}

			if (mediaEncoderProcess is not null)
			{
				mediaEncoderProcess.Exited -= OnProcessExit;

				mediaEncoderProcess.Kill(true);
				mediaEncoderProcess.Dispose();
				mediaEncoderProcess = null;
			}

			if (fileSystemWatcher is not null)
			{
				fileSystemWatcher.Created -= OnOutputFileCreated;
				fileSystemWatcher.Dispose();
				fileSystemWatcher = null;
			}
		}

		logger.LogDebug("Media encoder service instance '{Id}' disposed.", instanceId);
		disposedValue = true;
	}

	public void Dispose()
	{
		// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
		Dispose(true);
	}

	public async ValueTask DisposeAsync()
	{
		if (disposedValue) return;

		await Task.Run(Dispose);
	}

	#endregion
}
using System.Collections.Concurrent;

using DistributedRendering.AME.Server.Lib.Interfaces;
using DistributedRendering.AME.Server.Services.Interfaces;

namespace DistributedRendering.AME.Server.Services;

internal sealed class FileManagementService : IFileManagementService, IDisposable
{
	private readonly IConfigurationService configurationService;
	private readonly ILogger<FileManagementService> logger;
	private readonly IRenderQueueService queueService;

	private readonly DirectoryInfo projectDir;
	private readonly FileSystemWatcher projectsDirWatcher;
	private readonly ConcurrentDictionary<Guid, ICollection<FileInfo>> requiredFiles = [];
	private readonly ConcurrentDictionary<Guid, FileSystemWatcher> watchers = [];

	public FileManagementService(
		ILogger<FileManagementService> logger,
		IConfigurationService configurationService,
		IRenderQueueService queueService
	)
	{
		this.logger = logger;
		this.configurationService = configurationService;
		this.queueService = queueService;

		DirectoryInfo rootDir = configurationService.FileSettings.RootDataDirectory;

		if (!rootDir.Exists)
			throw new DirectoryNotFoundException(
				$"The root data directory does not exist at the configured path: '{rootDir.FullName}'."
			);

		projectDir = configurationService.FileSettings.ProjectsDirectory;

		if (!projectDir.Exists)
		{
			logger.LogWarning(
				"The project directory does not exist at the specified path '{Path}'. Attempting to create the missing directory.",
				projectDir
			);

			projectDir.Create();
			projectDir.Refresh();

			if (!projectDir.Exists)
				throw new DirectoryNotFoundException(
					$"The missing project directory at path '{projectDir.FullName}' could not be created."
				);
		}

		projectsDirWatcher = new FileSystemWatcher(projectDir.FullName)
		{
			EnableRaisingEvents = true,
			IncludeSubdirectories = false,
			NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.Size |
			               NotifyFilters.CreationTime
		};

		projectsDirWatcher.Created += OnNewProjectDirCreated;
	}

	public void Dispose()
	{
		if (watchers.IsEmpty)
			return;

		watchers
			.AsParallel()
			.ForAll(pair =>
				{
					FileSystemWatcher fileSystemWatcher = pair.Value;

					fileSystemWatcher.Created -= OnRenamed;
					fileSystemWatcher.Dispose();
				}
			);
	}

	public bool CreateWatcherForRequest(IRenderRequest request, ICollection<FileInfo> requiredFilesForRequest)
	{
		var watcher = new FileSystemWatcher
		{
			Path = request.RequestDirectory.FullName,
			EnableRaisingEvents = true,
			IncludeSubdirectories = true,
			NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName
		};

		watcher.Renamed += OnRenamed;

		requiredFiles.TryAdd(request.Guid, requiredFilesForRequest);

		logger.LogInformation("FileSystemWatcher created for directory '{Path}'.", watcher.Path);

		return watchers.TryAdd(request.Guid, watcher);
	}

	private void OnRenamed(object sender, FileSystemEventArgs e)
	{
		var renamedFile = new FileInfo(e.FullPath);

		if (renamedFile is not { Exists: true, Extension: ".mp4" })
			return;

		if (sender is not FileSystemWatcher watcher)
			throw new Exception(
				"Failed to handle 'OnRenamed' callback.",
				new InvalidCastException(
					$"The object invoking the '{nameof(OnRenamed)}' callback could not be cast to the required type '{nameof(FileSystemWatcher)}'."
				)
			);

		var watchedDir = new DirectoryInfo(watcher.Path);

		if (!Guid.TryParse(watchedDir.Name, out Guid guid))
			throw new Exception(
				"Failed to handle 'OnRenamed' callback.",
				new InvalidOperationException($"Failed to extract GUID from directory name '{watchedDir.Name}'.")
			);

		if (!requiredFiles.TryGetValue(guid, out ICollection<FileInfo>? fragmentFiles))
			throw new Exception(
				$"Failed to retrieve the number of required files for directory '{watchedDir.FullName}'."
			);

		fragmentFiles.AsParallel().ForAll(file => file.Refresh());
		if (fragmentFiles.Any(file => !file.Exists))
			return;

		logger.LogInformation(
			"All '{Count}' fragments for request '{Guid}' are present. Queueing request for reassembly...",
			fragmentFiles.Count,
			guid
		);

		queueService.QueueReassembly(guid);
		requiredFiles.TryRemove(guid, out _);
		watchers.TryRemove(guid, out _);

		watcher.Created -= OnRenamed;
		watcher.Dispose();
	}

	private void OnNewProjectDirCreated(object sender, FileSystemEventArgs eventArgs)
	{
		FileSettings fileSettings = configurationService.FileSettings;
		DirectoryInfo[] projects = projectDir.GetDirectories();
		int projectDirCount = projects.Length;
		long totalSize = projects.SelectMany(dirInfo => dirInfo.GetFiles().Select(file => file.Length)).Sum();

		while (projectDirCount >= fileSettings.MaxProjectDirCount)
		{
			logger.LogInformation("Projects directory exceeded maximum allowed count. Purging oldest directory...");

			PurgeOldestDirectory();
		}

		while (totalSize >= fileSettings.MaxProjectsDirSize)
		{
			logger.LogInformation("Projects directory exceeded maximum allowed size. Purging oldest directory...");

			PurgeOldestDirectory();
		}

		return;

		void PurgeOldestDirectory()
		{
			try
			{
				DirectoryInfo oldestProject = projects.OrderBy(info => info.CreationTimeUtc).First();

				oldestProject.Delete(true);
			}
			catch (Exception e)
			{
				logger.LogError(e, "Exception caught while attempting to purge old project directory.");
			}
		}
	}
}
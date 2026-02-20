using System.Text;

using DistributedRendering.AME.Server.Lib.Models;
using DistributedRendering.AME.Server.Services.Interfaces;
using DistributedRendering.AME.Shared.Enums;

using Microsoft.AspNetCore.Mvc;

namespace DistributedRendering.AME.Server.Controllers.api.v1;

[ApiController]
[Route("api/v1/[controller]")]
public sealed class RenderController(
	ILogger<RenderController> logger,
	IConfigurationService configurationService,
	IDatabaseService databaseService,
	IRequestSplittingService splittingService
) : Controller
{
	[HttpPost("submit")]
	public async Task<IActionResult> StartRender(
		[FromForm] string projectPath,
		[FromForm] TimeSpan timelineLength,
		[FromForm] TimeSpan jobLength = default
	)
	{
		#region Input Checks

		if (string.IsNullOrWhiteSpace(projectPath))
			return this.BadRequest("The project path cannot be empty.");

		if (timelineLength == TimeSpan.Zero)
			return this.BadRequest("The timeline length must be specified.");

		var projectDirectory = new DirectoryInfo(projectPath);

		if (projectDirectory is not { Exists: true })
			return this.BadRequest("The specified project path must be a valid path to an existing directory.");

		FileInfo? projectFile = projectDirectory
			.GetFiles()
			.FirstOrDefault(file => file.Extension.Equals(".prproj", StringComparison.InvariantCultureIgnoreCase));

		if (projectFile is not { Exists: true, IsReadOnly: false, Extension: ".prproj" })
			return this.BadRequest("The project directory did not contain a writable '.prproj' file.");

		FileInfo? presetFile = projectDirectory
			.GetFiles()
			.FirstOrDefault(file => file.Extension.Equals(".epr", StringComparison.InvariantCultureIgnoreCase));

		if (presetFile is not { Exists: true, IsReadOnly: false, Extension: ".epr" })
			return this.BadRequest("The project directory did not contain an '.epr' file.");

		jobLength = jobLength == TimeSpan.Zero ? configurationService.RenderHubSettings.JobLength : jobLength;

		#endregion

		var requestGuid = Guid.NewGuid();
		DirectoryInfo requestDir = Directory.CreateDirectory(
			Path.Combine(configurationService.FileSettings.RequestsDirectory.FullName, requestGuid.ToString())
		);

		var request = new RenderRequest
		{
			Guid = requestGuid,
			RequestDirectory = requestDir,
			ProjectPath = projectFile.FullName,
			EncoderPresetPath = presetFile.FullName,
			OutputPath = Path.Combine(requestDir.FullName, "output.mp4"),
			TimelineDuration = timelineLength,
			CreatedAt = DateTime.UtcNow
		};

		if (!await databaseService.CreateRenderRequestAsync(request))
		{
			logger.LogWarning("Failed to create render request in database.");

			return this.Problem(
				"Failed to create the render request. Please try again.",
				statusCode: StatusCodes.Status500InternalServerError
			);
		}

		await splittingService.SplitRequestAsync(request, jobLength);

		return this.Ok(new { requestGuid = request.Guid });
	}

	[HttpGet("totalRenderTime")]
	public async Task<IActionResult> GetRenderCompletionTime(Guid requestGuid)
	{
		if (requestGuid == Guid.Empty)
			return this.BadRequest("Please provide a valid GUID.");

		DbRequest? request = await databaseService.GetRenderRequestAsync(requestGuid);

		if (request is null)
			return this.BadRequest("The specified render request was not found.");

		if (request.CompletedAt == null)
			return this.Ok("The specified render request has not finished rendering yet.");

		return this.Ok(await GenerateResponseObject());

		async Task<object> GenerateResponseObject()
		{
			ICollection<DbReport> reports = await databaseService.GetAllReportsForRequestAsync(requestGuid);
			int fragmentCount = reports.Select(report => report.FragmentGuid).Distinct().Count();
			DbReport[] reportsWithFailureStatus =
				reports.Where(report => report.Status.HasFlag(RenderStatus.Failed)).ToArray();

			DateTime firstFragmentReceived =
				reports.Select(report => report.FragmentReceived).Where(dt => dt != default).Min();

			DateTime lastFragmentCompleted = reports.Select(report => report.ExportCompleted).Where(dt => dt != default)
				.Max();

			RenderReportTimestamps[] allTimestamps =
				reports.Select(report => new RenderReportTimestamps(report)).ToArray();

			RenderReportTimestamps[] validTimestamps = allTimestamps.Where(ts => ts.IsComplete).ToArray();

			TimeSpan averageLoadingTime =
				TimeSpan.FromMilliseconds(validTimestamps.Average(ts => ts.LoadingTime.TotalMilliseconds));

			TimeSpan maxLoadingTime = validTimestamps.Max(ts => ts.LoadingTime);
			TimeSpan minLoadingTime = validTimestamps.Min(ts => ts.LoadingTime);
			TimeSpan totalLoadingTime =
				TimeSpan.FromMilliseconds(validTimestamps.Sum(ts => ts.LoadingTime.TotalMilliseconds));

			TimeSpan averageRenderTime =
				TimeSpan.FromMilliseconds(validTimestamps.Average(ts => ts.RenderingTime.TotalMilliseconds));

			TimeSpan maxRenderTime = validTimestamps.Max(ts => ts.RenderingTime);
			TimeSpan minRenderTime = validTimestamps.Min(ts => ts.RenderingTime);
			TimeSpan totalRenderTime =
				TimeSpan.FromMilliseconds(validTimestamps.Sum(ts => ts.RenderingTime.TotalMilliseconds));

			TimeSpan averageEncodingTime =
				TimeSpan.FromMilliseconds(validTimestamps.Average(ts => ts.EncodingTime.TotalMilliseconds));

			TimeSpan maxEncodingTime = validTimestamps.Max(ts => ts.EncodingTime);
			TimeSpan minEncodingTime = validTimestamps.Min(ts => ts.EncodingTime);
			TimeSpan totalEncodingTime =
				TimeSpan.FromMilliseconds(validTimestamps.Sum(ts => ts.EncodingTime.TotalMilliseconds));

			TimeSpan averageTimeToExport =
				TimeSpan.FromMilliseconds(validTimestamps.Average(ts => ts.TimeToExport.TotalMilliseconds));

			TimeSpan maxTimeToExport = validTimestamps.Max(ts => ts.TimeToExport);
			TimeSpan minTimeToExport = validTimestamps.Min(ts => ts.TimeToExport);
			TimeSpan totalTimeToExport =
				TimeSpan.FromMilliseconds(validTimestamps.Sum(ts => ts.TimeToExport.TotalMilliseconds));

			return new
			{
				General = new
				{
					RequestGuid = requestGuid,
					FragmentCount = fragmentCount,
					ReportCount = reports.Count,
					ReportsWithFailureCode = reportsWithFailureStatus.Length,
					RequestSubmitted = request.CreatedAt,
					RequestCompleted = request.CompletedAt,
					FragmentTimeToExport = lastFragmentCompleted - firstFragmentReceived,
					TimeToExport = request.CompletedAt - request.CreatedAt,
					FirstFragmentDistributed = firstFragmentReceived.ToString("G"),
					LastFragmentCompleted = lastFragmentCompleted.ToString("G")
				},
				LoadingTimes = new
				{
					AverageLoadingTime = averageLoadingTime,
					MinimumLoadingTime = minLoadingTime,
					MaximumLoadingTIme = maxLoadingTime,
					TotalLoadingTime = totalLoadingTime
				},
				RenderTimes = new
				{
					AverageRenderTime = averageRenderTime,
					MinimumRenderTime = minRenderTime,
					MaximumRenderTime = maxRenderTime,
					TotalRenderTime = totalRenderTime
				},
				EncodingTimes = new
				{
					AverageEncodingTime = averageEncodingTime,
					MinimumEncodingTime = minEncodingTime,
					MaximumEncodingTime = maxEncodingTime,
					TotalEncodingTime = totalEncodingTime
				},
				TimeToExportTimes = new
				{
					AverageTimeToExport = averageTimeToExport,
					MinimumTimeToExport = minTimeToExport,
					MaximumTimeToExport = maxTimeToExport,
					TotalTimeToExport = totalTimeToExport
				}
			};
		}
	}

	[HttpGet("reportsCSV")]
	public async Task<IActionResult> GetReportsAsCsv(Guid requestGuid)
	{
		if (requestGuid == Guid.Empty)
			return this.BadRequest("Please provide a valid GUID.");

		DbRequest? request = await databaseService.GetRenderRequestAsync(requestGuid);

		if (request is null)
			return this.BadRequest("The specified render request was not found.");

		if (request.CompletedAt == null)
			return this.Ok("The specified render request has not finished rendering yet.");

		ICollection<DbReport> reports = await databaseService.GetAllReportsForRequestAsync(requestGuid);

		if (reports.Count == 0)
			return this.BadRequest("No reports were found for this request.");

		List<(DbReport report, RenderReportTimestamps)> timestamps = reports
			.Select(report => (report, new RenderReportTimestamps(report)))
			.Where(pair => pair.Item2.IsComplete)
			.ToList();

		var sb = new StringBuilder();
		sb.AppendLine("guid,fragment_guid,loading_time,render_time,encoding_time,time_to_export");

		foreach ((DbReport r, RenderReportTimestamps ts) in timestamps)
		{
			sb.AppendJoin(
				",",
				r.Guid,
				r.FragmentGuid,
				ts.LoadingTime.TotalMilliseconds,
				ts.RenderingTime.TotalMilliseconds,
				ts.EncodingTime.TotalMilliseconds,
				ts.TimeToExport.TotalMilliseconds
			);

			sb.AppendLine();
		}

		return this.Ok(sb.ToString());
	}
}
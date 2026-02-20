using System.Globalization;

using DistributedRendering.AME.Shared.DTOs;
using DistributedRendering.AME.Shared.Enums;
using DistributedRendering.AME.Shared.Interfaces;

// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global

namespace DistributedRendering.AME.Shared.Util;

public static class Extensions
{
	public static bool IsValid(this RenderFragment data)
	{
		if (data.Guid == Guid.Empty) return false;
		if (string.IsNullOrWhiteSpace(data.ProjectPath)) return false;
		if (string.IsNullOrWhiteSpace(data.PresetPath)) return false;
		if (string.IsNullOrWhiteSpace(data.DestinationPath)) return false;
		if (data.DurationInMilliseconds == 0) return false;

		return true;
	}

	public static string ToIso8601String(this DateTime dateTime)
	{
		return dateTime.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);
	}

	public static DateTime FromIso8601String(this string isoTimeString)
	{
		return DateTime.Parse(isoTimeString, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);
	}

	extension(IRenderReport? report)
	{
		public void AddOrAppendErrorMessage(string message)
		{
			if (report is null) return;

			if (report.ErrorMessage is null)
				report.ErrorMessage = message;
			else
				report.ErrorMessage += $"\n{message}";
		}

		public void AddFlagToReport(RenderStatus status)
		{
			if (report == null) return;

			if (!report.Status.HasFlag(status))
				report.Status |= status;
		}
	}
}
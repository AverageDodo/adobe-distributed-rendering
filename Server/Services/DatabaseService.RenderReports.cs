using System.Data;

using DistributedRendering.AME.Server.Lib.Models;
using DistributedRendering.AME.Shared.DTOs;
using DistributedRendering.AME.Shared.Enums;
using DistributedRendering.AME.Shared.Interfaces;
using DistributedRendering.AME.Shared.Util;

using Microsoft.Data.Sqlite;

namespace DistributedRendering.AME.Server.Services;

internal partial class DatabaseService
{
	public async ValueTask<bool> InsertRenderReportAsync(IRenderReport report, CancellationToken token = default)
	{
		if (token.IsCancellationRequested) return false;

		await using SqliteConnection connection = await GetConnectionAsync(token);
		await using SqliteCommand command = connection.CreateCommand();

		command.CommandText =
			"""
			INSERT INTO report 
			(guid, fk_fragment_guid, status, error_message, fragment_received, loading_started, render_started, encoding_started, export_completed) 
			VALUES 
			($0, $1, $2, $3, $4, $5, $6, $7, $8);
			""";

		command.Parameters.AddRange(
			[
				new SqliteParameter("$0", report.Guid.ToString()),
				new SqliteParameter("$1", report.Fragment.Guid.ToString()),
				new SqliteParameter("$2", (int)report.Status),
				new SqliteParameter(
					"$3",
					string.IsNullOrWhiteSpace(report.ErrorMessage) ? DBNull.Value : report.ErrorMessage
				),
				new SqliteParameter(
					"$4",
					report.FragmentReceived == default ? DBNull.Value : report.FragmentReceived.ToIso8601String()
				),
				new SqliteParameter(
					"$5",
					report.LoadingStarted == default ? DBNull.Value : report.LoadingStarted.ToIso8601String()
				),
				new SqliteParameter(
					"$6",
					report.RenderStarted == default ? DBNull.Value : report.RenderStarted.ToIso8601String()
				),
				new SqliteParameter(
					"$7",
					report.EncodingStarted == default ? DBNull.Value : report.EncodingStarted.ToIso8601String()
				),
				new SqliteParameter(
					"$8",
					report.ExportCompleted == default ? DBNull.Value : report.ExportCompleted.ToIso8601String()
				)
			]
		);

		var wasInserted = false;
		try
		{
			await connection.OpenAsync(token);

			wasInserted = await command.ExecuteNonQueryAsync(token) > 0;
		}
		catch (SqliteException e)
		{
			logger.LogError(e, "Exception caught while attempting to create render report.");
		}
		finally
		{
			if (connection.State is not ConnectionState.Closed)
				await connection.CloseAsync();
		}

		return wasInserted;
	}

	public async ValueTask<IRenderReport?> GetRenderReportAsync(Guid fragmentGuid, CancellationToken token = default)
	{
		if (token.IsCancellationRequested) return null;

		IRenderFragment? fragment = await GetFragmentAsync(fragmentGuid, token);
		if (fragment is not RenderFragment renderFragment)
		{
			logger.LogWarning("Attempted to retrieve render report instance for non-existent render fragment.");

			return null;
		}

		await using SqliteConnection connection = await GetConnectionAsync(token);
		await using SqliteCommand command = connection.CreateCommand();

		command.CommandText =
			"""
			SELECT * FROM report 
			WHERE fk_fragment_guid = $0;
			""";

		command.Parameters.AddWithValue("$0", fragmentGuid.ToString());

		IRenderReport? report = null;
		try
		{
			await connection.OpenAsync(token);
			await using SqliteDataReader reader = await command.ExecuteReaderAsync(token);

			if (await reader.ReadAsync(token))
				report = new RenderReport(renderFragment)
				{
					Guid = reader.GetGuid(0),
					Status = (RenderStatus)reader.GetInt32(2),
					ErrorMessage = reader.IsDBNull(3) ? reader.GetString(3) : null,
					FragmentReceived = reader.IsDBNull(4) ? reader.GetString(4).FromIso8601String() : default,
					LoadingStarted = reader.IsDBNull(5) ? reader.GetString(5).FromIso8601String() : default,
					RenderStarted = reader.IsDBNull(6) ? reader.GetString(6).FromIso8601String() : default,
					EncodingStarted = reader.IsDBNull(7) ? reader.GetString(7).FromIso8601String() : default,
					ExportCompleted = reader.IsDBNull(8) ? reader.GetString(8).FromIso8601String() : default
				};
		}
		catch (SqliteException e)
		{
			logger.LogError(e, "Exception caught while attempting to retrieve render report.");
		}
		finally
		{
			if (connection.State is not ConnectionState.Closed)
				await connection.CloseAsync();
		}

		return report;
	}

	public ValueTask<bool> UpdateRenderReportAsync(IRenderReport report, CancellationToken token = default)
	{
		logger.LogCritical("The method '{Name}' is not implemented.", nameof(UpdateRenderReportAsync));

		return ValueTask.FromResult(false);
	}

	public async ValueTask<ICollection<DbReport>> GetAllReportsForRequestAsync(
		Guid requestGuid,
		CancellationToken token = default
	)
	{
		if (token.IsCancellationRequested) return [];

		await using SqliteConnection connection = await GetConnectionAsync(token);
		await using SqliteCommand command = connection.CreateCommand();

		command.CommandText =
			"""
			SELECT report.* FROM request 
			JOIN fragment ON request.guid = fragment.fk_request_guid 
			JOIN report ON fragment.guid = report.fk_fragment_guid 
			WHERE request.guid = $0;
			""";

		command.Parameters.AddWithValue("$0", requestGuid.ToString());

		List<DbReport> reports = [];
		try
		{
			await connection.OpenAsync(token);
			await using SqliteDataReader reader = await command.ExecuteReaderAsync(token);

			while (await reader.ReadAsync(token))
				reports.Add(
					new DbReport
					{
						Guid = reader.GetGuid(0),
						FragmentGuid = reader.GetGuid(1),
						Status = (RenderStatus)reader.GetInt32(2),
						ErrorMessage = reader.IsDBNull(3) ? null : reader.GetString(3),
						FragmentReceived = reader.IsDBNull(4) ? default : reader.GetString(4).FromIso8601String(),
						LoadingStarted = reader.IsDBNull(5) ? default : reader.GetString(5).FromIso8601String(),
						RenderStarted = reader.IsDBNull(6) ? default : reader.GetString(6).FromIso8601String(),
						EncodingStarted = reader.IsDBNull(7) ? default : reader.GetString(7).FromIso8601String(),
						ExportCompleted = reader.IsDBNull(8) ? default : reader.GetString(8).FromIso8601String()
					}
				);

			await reader.CloseAsync();
		}
		catch (Exception e)
		{
			logger.LogError(e, "Exception caught while attempting to retrieve render report.");
		}
		finally
		{
			if (connection.State != ConnectionState.Closed)
				await connection.CloseAsync();
		}

		return reports;

		// ICollection<DbFragment> fragments = await GetFragmentsForRequestAsync(requestGuid, token);
		//
		// List<IRenderReport> reports = [];
		// foreach (DbFragment dbFragment in fragments)
		// {
		//     IRenderReport? report = await GetRenderReportAsync(dbFragment.Guid, token);
		//
		//     if (report is not null)
		//         reports.Add(report);
		// }
		//
		// return reports;
	}
}
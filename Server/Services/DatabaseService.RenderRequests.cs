using System.Data;

using DistributedRendering.AME.Server.Lib;
using DistributedRendering.AME.Server.Lib.Interfaces;
using DistributedRendering.AME.Server.Lib.Models;
using DistributedRendering.AME.Server.Lib.Util;
using DistributedRendering.AME.Shared.Util;

using Microsoft.Data.Sqlite;

namespace DistributedRendering.AME.Server.Services;

internal partial class DatabaseService
{
	public async ValueTask<bool> CreateRenderRequestAsync(IRenderRequest request, CancellationToken token = default)
	{
		if (!request.IsValid() || token.IsCancellationRequested) return false;

		await using SqliteConnection connection = await GetConnectionAsync(token);
		await using SqliteCommand command = connection.CreateCommand();

		command.CommandText =
			"""
			INSERT INTO request 
			(guid, requestDirectory, projectPath, encoderPresetPath, outputPath, timelineLength, createdAt) 
			VALUES 
			($0, $1, $2, $3, $4, $5, $6);
			""";

		command.Parameters.AddRange(
			[
				new SqliteParameter("$0", request.Guid.ToString()),
				new SqliteParameter("$1", request.RequestDirectory.FullName),
				new SqliteParameter("$2", request.ProjectPath),
				new SqliteParameter("$3", request.EncoderPresetPath),
				new SqliteParameter("$4", request.OutputPath),
				new SqliteParameter("$5", request.TimelineDuration.ToString("g")),
				new SqliteParameter("$6", request.CreatedAt.ToIso8601String())
			]
		);

		var wasInserted = false;

		try
		{
			await connection.OpenAsync(token);
			wasInserted = await command.ExecuteNonQueryAsync(token) != 0;
		}
		catch (SqliteException e)
		{
			logger.LogError(e, "Exception caught while attempting to insert new render request.");
		}
		finally
		{
			if (connection.State is not ConnectionState.Closed)
				await connection.CloseAsync();
		}

		return wasInserted;
	}

	public async ValueTask<DbRequest?> GetRenderRequestAsync(Guid guid, CancellationToken token = default)
	{
		if (guid == Guid.Empty || token.IsCancellationRequested) return null;

		await using SqliteConnection connection = await GetConnectionAsync(token);
		await using SqliteCommand command = connection.CreateCommand();

		command.CommandText =
			"""
			SELECT * FROM request 
			WHERE guid = $guid;
			""";

		command.Parameters.AddWithValue("$guid", guid.ToString());

		DbRequest? request = null;
		try
		{
			await connection.OpenAsync(token);

			await using SqliteDataReader reader = await command.ExecuteReaderAsync(token);
			if (await reader.ReadAsync(token))
				request = new DbRequest
				{
					Guid = reader.GetGuid(0),
					RequestDirectory = new DirectoryInfo(reader.GetString(1)),
					ProjectFile = new FileInfo(reader.GetString(2)),
					EncoderPresetFile = new FileInfo(reader.GetString(3)),
					OutputPath = new FileInfo(reader.GetString(4)),
					TimelineLength = reader.GetTimeSpan(5),
					CreatedAt = reader.GetString(6).FromIso8601String(),
					CompletedAt = reader.IsDBNull(7) ? default : reader.GetString(7).FromIso8601String()
				};
		}
		catch (SqliteException e)
		{
			logger.LogError(
				e,
				"Exception caught while attempting to retrieve render request with guid '{Guid}'.",
				guid
			);
		}
		finally
		{
			if (connection.State is not ConnectionState.Closed)
				await connection.CloseAsync();
		}

		return request;
	}

	public async ValueTask<bool> UpdateRenderRequestAsync(IRenderRequest request, CancellationToken token = default)
	{
		if (token.IsCancellationRequested) return false;

		await using SqliteConnection connection = await GetConnectionAsync(token);
		await using SqliteCommand command = connection.CreateCommand();

		command.CommandText =
			"""
			UPDATE request 
			SET (requestDirectory = $0, projectPath = $1, encoderPresetPath = $2, outputPath = $3, timelineLength = $4, createdAt = $5, completedAt = $6) 
			WHERE guid = $guid;
			""";

		command.Parameters.AddRange(
			[
				new SqliteParameter("$0", request.RequestDirectory.FullName),
				new SqliteParameter("$1", request.ProjectPath),
				new SqliteParameter("$2", request.EncoderPresetPath),
				new SqliteParameter("$3", request.OutputPath),
				new SqliteParameter("$4", request.TimelineDuration),
				new SqliteParameter("$5", request.CreatedAt.ToIso8601String()),
				new SqliteParameter(
					"$6",
					request.CompletedAt == default ? DBNull.Value : request.CompletedAt.ToIso8601String()
				),
				new SqliteParameter("$guid", request.Guid.ToString())
			]
		);

		var wasUpdated = false;

		try
		{
			await connection.OpenAsync(token);
			wasUpdated = await command.ExecuteNonQueryAsync(token) > 0;
		}
		catch (SqliteException e)
		{
			logger.LogError(
				e,
				"Exception caught while attempting to update render request with GUID '{Guid}'.",
				request.Guid
			);
		}
		finally
		{
			if (connection.State is not ConnectionState.Closed)
				await connection.CloseAsync();
		}

		return wasUpdated;
	}

	public async ValueTask<bool> MarkRequestCompleteAsync(
		Guid requestGuid,
		DateTime completedAt,
		CancellationToken token = default
	)
	{
		if (token.IsCancellationRequested) return false;

		await using SqliteConnection connection = await GetConnectionAsync(token);
		await using SqliteCommand command = connection.CreateCommand();

		Type type = typeof(DbRequest);
		DatabaseColumn completedColumn = type.GetColumn(nameof(DbRequest.CompletedAt));
		string guidColumn = type.GetColumnName(nameof(DbRequest.Guid));

		command.CommandText =
			$"""
			 UPDATE {type.GetTableName()} 
			 SET {completedColumn.Name} = '{completedAt.ToIso8601String()}' 
			 WHERE {guidColumn} = '{requestGuid.ToString()}'; 
			 """;

		var wasUpdated = false;
		try
		{
			await connection.OpenAsync(token);
			wasUpdated = await command.ExecuteNonQueryAsync(token) > 0;
			await connection.CloseAsync();
		}
		catch (SqliteException e)
		{
			logger.LogError(e, "Exception caught while attempting to update completion time.");
		}
		finally
		{
			if (connection.State is not ConnectionState.Closed)
				await connection.CloseAsync();
		}

		return wasUpdated;
	}
}
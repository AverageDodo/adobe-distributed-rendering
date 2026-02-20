using System.Data;

using DistributedRendering.AME.Server.Lib.Models;
using DistributedRendering.AME.Shared.DTOs;

using Microsoft.Data.Sqlite;

namespace DistributedRendering.AME.Server.Services;

internal partial class DatabaseService
{
	public async ValueTask<IRenderFragment?> GetFragmentAsync(Guid fragmentGuid, CancellationToken token = default)
	{
		if (token.IsCancellationRequested) return null;

		await using SqliteConnection connection = await GetConnectionAsync(token);
		await using SqliteCommand command = connection.CreateCommand();

		command.CommandText =
			"""
			SELECT * FROM fragment 
			WHERE guid = $0;    
			""";

		command.Parameters.AddWithValue("$0", fragmentGuid.ToString());

		DbFragment? dbFragment = null;
		try
		{
			await connection.OpenAsync(token);
			await using SqliteDataReader reader = await command.ExecuteReaderAsync(token);

			if (await reader.ReadAsync(token))
				dbFragment = new DbFragment
				{
					Guid = reader.GetGuid(0),
					RequestGuid = reader.GetGuid(1),
					OutputPath = new FileInfo(reader.GetString(2)),
					Position = reader.GetInt32(3),
					StartTimeInMilliseconds = reader.GetInt64(4),
					DurationInMilliseconds = reader.GetInt64(5)
				};
		}
		catch (SqliteException e)
		{
			logger.LogError(e, "Exception caught while attempting to LogMessage.");
		}
		finally
		{
			if (connection.State is not ConnectionState.Closed)
				await connection.CloseAsync();
		}

		if (dbFragment is null) return null;

		DbRequest? request = await GetRenderRequestAsync(dbFragment.RequestGuid, token);

		return request is null
			? null
			: new RenderFragment
			{
				Guid = dbFragment.Guid,
				RequestGuid = dbFragment.RequestGuid,
				ProjectPath = request.ProjectFile.FullName,
				PresetPath = request.EncoderPresetFile.FullName,
				DestinationPath = dbFragment.OutputPath.FullName,
				Index = dbFragment.Position,
				StartTimeInMilliseconds = dbFragment.StartTimeInMilliseconds,
				DurationInMilliseconds = (int)dbFragment.DurationInMilliseconds
			};
	}

	public async ValueTask<bool> InsertFragmentAsync(RenderFragment fragment, CancellationToken token = default)
	{
		if (token.IsCancellationRequested) return false;

		await using SqliteConnection connection = await GetConnectionAsync(token);
		await using SqliteCommand insertCommand = connection.CreateCommand();
		insertCommand.CommandText =
			"""
			INSERT INTO fragment 
			(guid, fk_request_guid, outputPath, position, start_time, duration) 
			VALUES 
			($0, $1, $2, $3, $4, $5);
			""";

		insertCommand.Parameters.AddRange(
			[
				new SqliteParameter("$0", fragment.Guid.ToString()),
				new SqliteParameter("$1", fragment.RequestGuid.ToString()),
				new SqliteParameter("$2", fragment.DestinationPath),
				new SqliteParameter("$3", fragment.Index),
				new SqliteParameter("$4", fragment.StartTimeInMilliseconds),
				new SqliteParameter("$5", fragment.DurationInMilliseconds)
			]
		);

		var wasInserted = false;
		try
		{
			await connection.OpenAsync(token);

			wasInserted = await insertCommand.ExecuteNonQueryAsync(token) != 0;

			await connection.CloseAsync();
		}
		catch (SqliteException e)
		{
			logger.LogError(e, "Exception caught while attempting to insert render fragment.");
		}
		finally
		{
			if (connection.State is not ConnectionState.Closed)
				await connection.CloseAsync();
		}

		return wasInserted;
	}

	public async Task<ICollection<DbFragment>> GetFragmentsForRequestAsync(
		Guid requestGuid,
		CancellationToken token = default
	)
	{
		DbRequest? request = await GetRenderRequestAsync(requestGuid, token);

		if (request is null)
		{
			logger.LogWarning("Attempting to retrieve fragments for non-existent render request.");

			return [];
		}

		await using SqliteConnection connection = await GetConnectionAsync(token);
		await using SqliteCommand command = connection.CreateCommand();

		command.CommandText =
			"""
			SELECT * FROM fragment 
			WHERE fk_request_guid = $0
			ORDER BY position;
			""";

		command.Parameters.AddWithValue("$0", requestGuid.ToString());

		List<DbFragment> fragments = [];
		try
		{
			await connection.OpenAsync(token);

			await using SqliteDataReader reader = await command.ExecuteReaderAsync(token);
			while (await reader.ReadAsync(token))
			{
				var fragment = new DbFragment
				{
					Guid = reader.GetGuid(0),
					RequestGuid = reader.GetGuid(1),
					OutputPath = new FileInfo(reader.GetString(2)),
					Position = reader.GetInt32(3),
					StartTimeInMilliseconds = reader.GetInt64(4),
					DurationInMilliseconds = reader.GetInt64(5)
				};

				fragments.Insert(fragment.Position, fragment);
			}

			await connection.CloseAsync();
		}
		catch (SqliteException e)
		{
			logger.LogError(
				e,
				"Exception caught while attempting to retrieve fragments for request '{Guid}'.",
				requestGuid
			);
		}
		finally
		{
			if (connection.State is not ConnectionState.Closed)
				await connection.CloseAsync();
		}

		int missingFragmentFiles = fragments.Count(f => f.OutputPath.Exists);
		if (missingFragmentFiles != 0)
			logger.LogWarning("'{Count}' fragment(s) have an output path that does not exist.", missingFragmentFiles);

		return fragments;
	}
}
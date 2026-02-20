using System.Collections.Concurrent;
using System.Data;

using DistributedRendering.AME.Server.Services.Interfaces;

using Microsoft.Data.Sqlite;

namespace DistributedRendering.AME.Server.Lib.Util;

public class SqliteLogDatabaseProvider : IAsyncDisposable
{
	private readonly CancellationTokenSource cancellationTokenSource;
	private readonly DatabaseLoggerSettings configuration;
	private readonly ConcurrentQueue<DbLogEntry> logEntryQueue = [];

	public SqliteLogDatabaseProvider(
		DatabaseLoggerSettings databaseLoggerSettings,
		CancellationToken cancellationToken = default
	)
	{
		configuration = databaseLoggerSettings;

		cancellationTokenSource = cancellationToken == CancellationToken.None
			? new CancellationTokenSource()
			: CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

		_ = Task.Run(WriteToDatabaseAsync, cancellationTokenSource.Token);

		try
		{
			using var connection = new SqliteConnection(configuration.ConnectionString);
			using SqliteCommand command = connection.CreateCommand();

			int rowCountLimit = configuration.RowCountLimit > 0 ? configuration.RowCountLimit : int.MaxValue;
			command.CommandText =
				$"""
				 CREATE TABLE IF NOT EXISTS log_entry(
				     timestamp TEXT NOT NULL, 
				     level INTEGER UNSIGNED NOT NULL,
				     category TEXT,
				     message TEXT,
				     exception TEXT
				 );

				 CREATE TRIGGER IF NOT EXISTS row_limit_trigger 
				 	BEFORE INSERT ON log_entry 
				 	WHEN (SELECT COUNT(*) FROM log_entry) > {rowCountLimit} 
				 	BEGIN 
				 		DELETE FROM log_entry WHERE ROWID = (SELECT min(ROWID) FROM log_entry);
				 	END;
				 """;

			connection.Open();
			command.ExecuteNonQuery();
			connection.Close();
		}
		catch (Exception e)
		{
			Console.WriteLine(e);
		}
	}

	public void QueueLogEntry(DbLogEntry logEntry)
	{
		logEntryQueue.Enqueue(logEntry);
	}

	private async Task WriteToDatabaseAsync()
	{
		try
		{
			while (!cancellationTokenSource.IsCancellationRequested)
			{
				if (!logEntryQueue.TryDequeue(out DbLogEntry entry))
				{
					await Task.Delay(TimeSpan.FromSeconds(1), cancellationTokenSource.Token);
					continue;
				}

				await using var connection = new SqliteConnection(configuration.ConnectionString);
				await using SqliteCommand command = connection.CreateCommand();

				command.CommandText =
					"""
					INSERT INTO log_entry 
					    (timestamp, level, category, message, exception)
					VALUES 
						($0, $1, $2, $3, $4);
					""";

				command.Parameters.AddRange(
					[
						new SqliteParameter("$0", entry.TimeStamp),
						new SqliteParameter("$1", entry.Level),
						new SqliteParameter("$2", string.IsNullOrEmpty(entry.Category) ? DBNull.Value : entry.Category),
						new SqliteParameter("$3", string.IsNullOrEmpty(entry.Message) ? DBNull.Value : entry.Message),
						new SqliteParameter(
							"$4",
							string.IsNullOrEmpty(entry.Exception) ? DBNull.Value : entry.Exception
						)
					]
				);

				try
				{
					await connection.OpenAsync(cancellationTokenSource.Token);
					await command.ExecuteNonQueryAsync(cancellationTokenSource.Token);
					await connection.CloseAsync();
				}
				catch (Exception e)
				{
					Console.WriteLine(e);
				}
				finally
				{
					if (connection.State != ConnectionState.Closed)
						await connection.CloseAsync();
				}
			}
		}
		catch (Exception e)
		{
			Console.WriteLine(e);
		}
	}

	public async ValueTask DisposeAsync()
	{
		GC.SuppressFinalize(this);

		await cancellationTokenSource.CancelAsync();
		cancellationTokenSource.Dispose();
	}
}
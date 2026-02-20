using System.Data;

using DistributedRendering.AME.Server.Services.Interfaces;

using Microsoft.Data.Sqlite;

namespace DistributedRendering.AME.Server.Services;

internal sealed partial class DatabaseService : IDatabaseService
{
	private const string c_DbCreateQuery =
		"""
		CREATE TABLE IF NOT EXISTS request (
		    guid TEXT PRIMARY KEY,
		    requestDirectory TEXT NOT NULL,
		    projectPath TEXT NOT NULL,
		    encoderPresetPath TEXT NOT NULL,
		    outputPath TEXT NOT NULL,
		    timelineLength INTEGER UNSIGNED NOT NULL,
		    createdAt TEXT NOT NULL,
		    completedAt TEXT
		);

		CREATE TABLE IF NOT EXISTS fragment (
		    guid TEXT PRIMARY KEY,
		    fk_request_guid TEXT NOT NULL,
		    outputPath TEXT NOT NULL,
		    position INTEGER UNSIGNED NOT NULL,
		    start_time INTEGER UNSIGNED NOT NULL, 
		    duration INTEGER UNSIGNED NOT NULL, 
		    FOREIGN KEY (fk_request_guid) REFERENCES request(guid)
		        ON UPDATE CASCADE ON DELETE CASCADE
		);

		CREATE TABLE IF NOT EXISTS report (
		    guid TEXT PRIMARY KEY, 
		    fk_fragment_guid TEXT NOT NULL, 
		    status INTEGER UNSIGNED NOT NULL, 
		    error_message TEXT, 
		    fragment_received TEXT,
		    loading_started TEXT,
		    render_started TEXT,
		    encoding_started TEXT,
		    export_completed TEXT,
		    FOREIGN KEY (fk_fragment_guid) REFERENCES fragment(guid) 
		        ON UPDATE CASCADE ON DELETE CASCADE
		);
		""";

	private readonly string connectionString;
	private readonly Task dbInitTask;
	private readonly ILogger<DatabaseService> logger;

	public DatabaseService(
		ILogger<DatabaseService> logger,
		IConfigurationService configurationService
	)
	{
		this.logger = logger;

		connectionString = configurationService.DatabaseSettings.ConnectionString;

		dbInitTask = Task.Run(async () =>
			{
				var connStr = new SqliteConnectionStringBuilder(connectionString);
				if (configurationService.DatabaseSettings.ClearDatabaseOnRestart && File.Exists(connStr.DataSource))
				{
					logger.LogInformation("Database reset is enabled. Deleting old database file...");
					File.Delete(connStr.DataSource);
				}

				await using var connection = new SqliteConnection(connectionString);
				await using SqliteCommand createTablesCommand = connection.CreateCommand();
				createTablesCommand.CommandText = c_DbCreateQuery;

				try
				{
					await connection.OpenAsync();
					await createTablesCommand.ExecuteNonQueryAsync();

					logger.LogInformation("Database initialization complete.");
				}
				catch (SqliteException e)
				{
					logger.LogError(e, "Exception caught while attempting to initialize the database.");
				}
				finally
				{
					if (connection.State is not ConnectionState.Closed)
						await connection.CloseAsync();
				}
			}
		);
	}

	private async ValueTask<SqliteConnection> GetConnectionAsync(CancellationToken token = default)
	{
		if (!dbInitTask.IsCompleted)
			await dbInitTask.WaitAsync(token);

		return new SqliteConnection(connectionString);
	}
}
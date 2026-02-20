using DistributedRendering.AME.Client.Services.Interfaces;
using DistributedRendering.AME.Shared.Enums;

namespace DistributedRendering.AME.Client.Services;

internal sealed class StateService(
	ILogger<StateService> logger
) : IStateService
{
	private static readonly ReaderWriterLockSlim Lock = new();
	private ClientStatus status;

	public event EventHandler<ClientStatus> StatusChanged = (_, updatedStatus) =>
	{
		logger.LogDebug("StatusChangedEvent invoked: '{Status}'.", updatedStatus);
	};

	public event IStateService.AsyncEventHandler<ClientStatus>? StatusChangedAsync;

	private async Task RaiseStatusChangedAsync(ClientStatus updatedStatus)
	{
		IStateService.AsyncEventHandler<ClientStatus>? handlers = StatusChangedAsync;
		if (handlers == null) return;

		IEnumerable<Task> tasks = handlers.GetInvocationList()
			.Cast<IStateService.AsyncEventHandler<ClientStatus>>()
			.Select(async handler =>
				{
					try
					{
						await handler(this, updatedStatus).ConfigureAwait(false);
					}
					catch (Exception ex)
					{
						logger.LogError(ex, "Async status handler failed.");
					}
				}
			).ToArray();

		await Task.WhenAll(tasks).ConfigureAwait(false);
	}

	public bool GetStatus(out ClientStatus currentStatus)
	{
		currentStatus = ClientStatus.Unavailable;

		var readStatus = false;
		try
		{
			readStatus = Lock.TryEnterReadLock(500);
			currentStatus = status;
		}
		catch (Exception e)
		{
			logger.LogError(e, "Exception caught while attempting to enter a read lock.");
		}
		finally
		{
			if (Lock.IsReadLockHeld)
				Lock.ExitReadLock();
		}

		return readStatus;
	}

	public bool SetStatus(ClientStatus value, out ClientStatus updatedStatus)
	{
		updatedStatus = status;

		var wasUpdated = false;
		try
		{
			Lock.EnterUpgradeableReadLock();

			if (value == status)
			{
				wasUpdated = true;

				return wasUpdated;
			}
			else
			{
				Lock.EnterWriteLock();

				status = value;
				updatedStatus = status;

				wasUpdated = status == value;
			}
		}
		catch (Exception e)
		{
			logger.LogError(e, "Exception caught while attempting to enter a write lock.");
		}
		finally
		{
			if (Lock.IsUpgradeableReadLockHeld)
				Lock.ExitUpgradeableReadLock();

			if (Lock.IsWriteLockHeld)
				Lock.ExitWriteLock();

			StatusChanged.Invoke(this, updatedStatus);
			_ = RaiseStatusChangedAsync(updatedStatus);
		}

		return wasUpdated;
	}
}
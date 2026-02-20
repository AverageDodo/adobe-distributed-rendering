using DistributedRendering.AME.Shared.Enums;

namespace DistributedRendering.AME.Client.Services.Interfaces;

public interface IStateService
{
	event EventHandler<ClientStatus> StatusChanged;
	event AsyncEventHandler<ClientStatus> StatusChangedAsync;
	delegate Task AsyncEventHandler<in TEventArgs>(object? sender, TEventArgs eventArgs);
	bool GetStatus(out ClientStatus currentStatus);

	bool SetStatus(ClientStatus value, out ClientStatus updatedStatus);
}
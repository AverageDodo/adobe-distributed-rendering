using DistributedRendering.AME.Shared.DTOs;
using DistributedRendering.AME.Shared.Enums;

namespace DistributedRendering.AME.Shared.Interfaces;

public interface IRenderReport
{
    Guid Guid { get; }
    RenderFragment Fragment { get; }
    RenderStatus Status { get; set; }
    string? ErrorMessage { get; set; }
    DateTime FragmentReceived { get; set; }
    DateTime LoadingStarted { get; set; }
    DateTime RenderStarted { get; set; }
    DateTime EncodingStarted { get; set; }
    DateTime ExportCompleted { get; set; }
}
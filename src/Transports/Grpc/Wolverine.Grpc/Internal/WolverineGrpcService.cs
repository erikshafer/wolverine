using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace Wolverine.Transports.Grpc.Internal;

/// <summary>
/// The ASP.NET Core gRPC service that handles incoming Wolverine messages.
/// It delegates envelope processing to the associated <see cref="GrpcListener"/>.
/// </summary>
internal sealed class WolverineGrpcService : WolverineGrpc.WolverineGrpcBase
{
    private readonly GrpcListener _listener;
    private readonly ILogger<WolverineGrpcService> _logger;

    public WolverineGrpcService(GrpcListener listener, ILogger<WolverineGrpcService> logger)
    {
        _listener = listener;
        _logger = logger;
    }

    public override async Task<Ack> Send(EnvelopeRequest request, ServerCallContext context)
    {
        try
        {
            await _listener.HandleAsync(request.Data.ToByteArray(), context.CancellationToken);
            return new Ack { Success = true };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing incoming gRPC envelope batch");
            return new Ack { Success = false, Error = ex.Message };
        }
    }

    public override Task<Ack> Ping(PingRequest request, ServerCallContext context)
    {
        return Task.FromResult(new Ack { Success = true });
    }
}

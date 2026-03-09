using Messages;
using Microsoft.Extensions.Logging;
using Wolverine;

namespace Ponger;

// #region sample_PingHandler_Grpc

/// <summary>
/// Handles incoming Ping messages received via gRPC and responds with a Pong
/// back to the originating Pinger service, also via gRPC.
/// </summary>
public class PingHandler
{
    public ValueTask Handle(Ping ping, ILogger<PingHandler> logger, IMessageContext context)
    {
        logger.LogInformation("Got Ping #{Number} via gRPC", ping.Number);
        return context.RespondToSenderAsync(new Pong { Number = ping.Number });
    }
}

// #endregion

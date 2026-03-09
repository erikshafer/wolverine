using Messages;

namespace Pinger;

// #region sample_PongHandler_Grpc

/// <summary>
/// Handles Pong responses sent back from the Ponger service via gRPC.
/// </summary>
public class PongHandler
{
    public void Handle(Pong pong, ILogger<PongHandler> logger)
    {
        logger.LogInformation("Received Pong #{Number} via gRPC", pong.Number);
    }
}

// #endregion

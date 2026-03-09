using Microsoft.Extensions.Logging;
using Wolverine.Runtime.Serialization;
using Wolverine.Transports;

namespace Wolverine.Transports.Grpc.Internal;

/// <summary>
/// Pure, stateless helper that owns the business logic for deserializing
/// an incoming gRPC envelope batch and dispatching it to the Wolverine
/// receiver pipeline.
/// Extracted from <see cref="GrpcListener"/> so the logic can be unit-tested
/// in complete isolation from the Kestrel server lifecycle.
/// </summary>
internal static class GrpcEnvelopeHandler
{
    /// <summary>
    /// Deserializes <paramref name="data"/> into Wolverine envelopes and forwards
    /// real message batches to <paramref name="receiver"/>.
    /// </summary>
    /// <remarks>
    /// Three special cases are handled silently (no receiver call):
    /// <list type="bullet">
    ///   <item><description>Empty byte array → nothing to do</description></item>
    ///   <item><description>Zero envelopes after deserialization → nothing to do</description></item>
    ///   <item><description>Single ping-only envelope → connectivity probe, silently dropped</description></item>
    /// </list>
    /// Any other deserialization failure is logged and re-thrown so the caller
    /// (<see cref="WolverineGrpcService"/>) can return an error Ack to the sender.
    /// </remarks>
    internal static async ValueTask HandleAsync(
        byte[] data,
        IListener listener,
        IReceiver receiver,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        if (data.Length == 0)
        {
            return;
        }

        Envelope[] envelopes;

        try
        {
            envelopes = EnvelopeSerializer.ReadMany(data);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to deserialize incoming gRPC envelope batch");
            throw;
        }

        if (envelopes.Length == 0)
        {
            return;
        }

        // A batch that contains only a ping envelope is a connectivity probe — drop it.
        if (envelopes.Length == 1 && envelopes[0].IsPing())
        {
            return;
        }

        await receiver.ReceivedAsync(listener, envelopes);
    }
}

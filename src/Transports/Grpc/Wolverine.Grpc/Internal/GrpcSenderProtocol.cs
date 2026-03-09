using Google.Protobuf;
using Grpc.Net.Client;
using Microsoft.Extensions.Logging;
using Wolverine.Transports.Sending;

namespace Wolverine.Transports.Grpc.Internal;

/// <summary>
/// Sends outgoing Wolverine envelope batches to a remote gRPC endpoint.
/// Each <see cref="GrpcSenderProtocol"/> instance manages its own
/// <see cref="GrpcChannel"/> to the configured destination.
/// </summary>
/// <remarks>
/// The HTTP scheme used for the underlying gRPC channel is derived from the
/// Wolverine endpoint URI scheme:
/// <list type="bullet">
///   <item><description><c>grpc://</c> → <c>http://</c> (plain HTTP/2)</description></item>
///   <item><description><c>grpcs://</c> → <c>https://</c> (TLS)</description></item>
/// </list>
/// </remarks>
public sealed class GrpcSenderProtocol : ISenderProtocol, IDisposable
{
    private readonly Uri _destination;
    private readonly ILogger<GrpcSenderProtocol> _logger;
    private readonly GrpcChannel _channel;
    private readonly WolverineGrpc.WolverineGrpcClient _client;

    /// <summary>The HTTP/HTTPS address used for the underlying gRPC channel.</summary>
    internal string ChannelAddress { get; }

    public GrpcSenderProtocol(Uri destination, ILogger<GrpcSenderProtocol> logger)
    {
        _destination = destination;
        _logger = logger;

        // Map grpc:// → http:// and grpcs:// → https:// so the underlying
        // gRPC channel uses the correct transport security.
        var httpScheme = destination.Scheme == GrpcSecureTransport.ProtocolName ? "https" : "http";
        ChannelAddress = $"{httpScheme}://{destination.Host}:{destination.Port}";

        _channel = GrpcChannel.ForAddress(ChannelAddress);
        _client = new WolverineGrpc.WolverineGrpcClient(_channel);
    }

    public async Task SendBatchAsync(ISenderCallback callback, OutgoingMessageBatch batch)
    {
        if (batch.Data.Length == 0)
        {
            // Nothing to send; treat as a no-op success so the batching
            // pipeline doesn't log spurious errors.
            await callback.MarkSuccessfulAsync(batch);
            return;
        }

        try
        {
            var request = new EnvelopeRequest
            {
                Data = ByteString.CopyFrom(batch.Data)
            };

            var ack = await _client.SendAsync(request);

            if (ack.Success)
            {
                await callback.MarkSuccessfulAsync(batch);
            }
            else
            {
                _logger.LogWarning(
                    "Remote gRPC endpoint {Destination} rejected message batch: {Error}",
                    _destination,
                    ack.Error);
                await callback.MarkProcessingFailureAsync(batch);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send gRPC message batch to {Destination}", _destination);
            await callback.MarkProcessingFailureAsync(batch, ex);
        }
    }

    public void Dispose()
    {
        _channel.Dispose();
    }
}

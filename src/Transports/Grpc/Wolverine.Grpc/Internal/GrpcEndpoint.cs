using JasperFx.Core;
using Microsoft.Extensions.Logging;
using Wolverine.Configuration;
using Wolverine.Runtime;
using Wolverine.Transports;
using Wolverine.Transports.Sending;

namespace Wolverine.Transports.Grpc.Internal;

/// <summary>
/// Represents a single gRPC endpoint, identified by host and port.
/// A <see cref="GrpcEndpoint"/> can act as either a listener (server) or a sender (client).
/// </summary>
public class GrpcEndpoint : Endpoint
{
    private readonly GrpcTransport _transport;

    public GrpcEndpoint(Uri uri, GrpcTransport transport) : base(uri, EndpointRole.Application)
    {
        _transport = transport;
        HostName = uri.Host;
        Port = uri.Port > 0 ? uri.Port : GrpcTransportExtensions.DefaultPort;

        // ReSharper disable once VirtualMemberCallInConstructor
        EndpointName = uri.ToString();
        Mode = EndpointMode.BufferedInMemory;
    }

    public string HostName { get; }

    public int Port { get; }

    /// <summary>
    /// Builds the canonical <c>grpc://</c> URI for a given host and port.
    /// </summary>
    public static Uri ToUri(string hostName, int port) =>
        new Uri($"grpc://{hostName}:{port}");

    protected override bool supportsMode(EndpointMode mode) => mode != EndpointMode.Inline;

    public override ValueTask<IListener> BuildListenerAsync(IWolverineRuntime runtime, IReceiver receiver)
    {
        var logger = runtime.LoggerFactory.CreateLogger<GrpcListener>();
        var listener = new GrpcListener(this, receiver, logger, runtime.DurabilitySettings.Cancellation);
        return ValueTask.FromResult<IListener>(listener);
    }

    protected override ISender CreateSender(IWolverineRuntime runtime)
    {
        return new BatchedSender(
            this,
            new GrpcSenderProtocol(Uri, runtime.LoggerFactory.CreateLogger<GrpcSenderProtocol>()),
            runtime.DurabilitySettings.Cancellation,
            runtime.LoggerFactory.CreateLogger<GrpcSenderProtocol>());
    }
}

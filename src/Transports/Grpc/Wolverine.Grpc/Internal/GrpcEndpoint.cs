using JasperFx.Core;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography.X509Certificates;
using Wolverine.Configuration;
using Wolverine.Runtime;
using Wolverine.Transports;
using Wolverine.Transports.Sending;

namespace Wolverine.Transports.Grpc.Internal;

/// <summary>
/// Represents a single gRPC endpoint, identified by host and port.
/// A <see cref="GrpcEndpoint"/> can act as either a listener (server) or a sender (client).
/// The URI scheme determines whether TLS is used:
/// <list type="bullet">
///   <item><description><c>grpc://</c> — plain HTTP/2, no TLS</description></item>
///   <item><description><c>grpcs://</c> — HTTP/2 over TLS (HTTPS)</description></item>
/// </list>
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
    /// Returns <c>true</c> when this endpoint uses TLS (i.e., the URI scheme is <c>grpcs://</c>).
    /// </summary>
    public bool IsTls => Uri.Scheme == GrpcSecureTransport.ProtocolName;

    /// <summary>
    /// X.509 certificate used to configure Kestrel for TLS when <see cref="IsTls"/> is <c>true</c>
    /// and this endpoint is acting as a listener.
    /// When <c>null</c>, Wolverine attempts to use the ASP.NET Core HTTPS development certificate.
    /// </summary>
    public X509Certificate2? TlsCertificate { get; set; }

    /// <summary>
    /// Builds the canonical <c>grpc://</c> (plain) URI for a given host and port.
    /// </summary>
    public static Uri ToUri(string hostName, int port) =>
        new Uri($"grpc://{hostName}:{port}");

    /// <summary>
    /// Builds the canonical <c>grpcs://</c> (TLS) URI for a given host and port.
    /// </summary>
    public static Uri ToSecureUri(string hostName, int port) =>
        new Uri($"grpcs://{hostName}:{port}");

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

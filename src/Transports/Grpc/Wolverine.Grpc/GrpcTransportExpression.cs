using JasperFx.Core.Reflection;
using System.Security.Cryptography.X509Certificates;
using Wolverine.Configuration;
using Wolverine.Transports.Grpc.Internal;

namespace Wolverine.Transports.Grpc;

/// <summary>
/// Fluent configuration expression for the gRPC transport.
/// </summary>
public class GrpcTransportExpression
{
    private readonly GrpcTransport _transport;
    private readonly GrpcSecureTransport _secureTransport;
    private readonly WolverineOptions _options;

    internal GrpcTransportExpression(GrpcTransport transport, WolverineOptions options)
    {
        _transport = transport;
        _secureTransport = options.Transports.GetOrCreate<GrpcSecureTransport>();
        _options = options;
    }

    /// <summary>
    /// Configures Wolverine to listen for incoming plain gRPC messages (HTTP/2, no TLS)
    /// on the specified port.
    /// </summary>
    /// <param name="port">The TCP port on which to start the gRPC server.</param>
    /// <returns>The current expression for further configuration (fluent).</returns>
    public GrpcTransportExpression ListenOnPort(int port)
    {
        var endpoint = _transport.EndpointFor("localhost", port);
        endpoint.IsListener = true;
        return this;
    }

    /// <summary>
    /// Configures Wolverine to listen for incoming TLS-secured gRPC messages
    /// (<c>grpcs://</c>, HTTP/2 over HTTPS) on the specified port.
    /// </summary>
    /// <param name="port">The TCP port on which to start the secure gRPC server.</param>
    /// <param name="certificate">
    /// Optional X.509 certificate. When <c>null</c>, the ASP.NET Core HTTPS development
    /// certificate is used (suitable for development environments only).
    /// </param>
    /// <returns>The current expression for further configuration (fluent).</returns>
    public GrpcTransportExpression ListenOnPortWithTls(int port, X509Certificate2? certificate = null)
    {
        var endpoint = _secureTransport.EndpointFor("localhost", port);
        endpoint.IsListener = true;
        endpoint.TlsCertificate = certificate;
        return this;
    }

    /// <summary>
    /// Configures Wolverine to send messages to the specified remote gRPC endpoint
    /// using plain HTTP/2 (no TLS).
    /// Routes all published messages of any type to the specified gRPC address.
    /// </summary>
    /// <param name="host">The remote host name or IP address.</param>
    /// <param name="port">The remote TCP port.</param>
    /// <returns>A subscriber configuration for further customization.</returns>
    public ISubscriberConfiguration SendTo(string host, int port = GrpcTransportExtensions.DefaultPort)
    {
        var uri = GrpcEndpoint.ToUri(host, port);
        _transport.GetOrCreateEndpoint(uri);
        return _options.PublishAllMessages().To(uri);
    }

    /// <summary>
    /// Configures Wolverine to send messages to the specified remote gRPC endpoint
    /// using TLS-secured HTTP/2 (<c>grpcs://</c>).
    /// </summary>
    /// <param name="host">The remote host name or IP address.</param>
    /// <param name="port">The remote TCP port.</param>
    /// <returns>A subscriber configuration for further customization.</returns>
    public ISubscriberConfiguration SendToWithTls(string host, int port = GrpcTransportExtensions.DefaultPort)
    {
        var uri = GrpcEndpoint.ToSecureUri(host, port);
        _secureTransport.GetOrCreateEndpoint(uri);
        return _options.PublishAllMessages().To(uri);
    }
}

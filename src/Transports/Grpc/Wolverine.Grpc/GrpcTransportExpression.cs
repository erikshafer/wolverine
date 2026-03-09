using JasperFx.Core.Reflection;
using Wolverine.Configuration;
using Wolverine.Transports.Grpc.Internal;

namespace Wolverine.Transports.Grpc;

/// <summary>
/// Fluent configuration expression for the gRPC transport.
/// </summary>
public class GrpcTransportExpression
{
    private readonly GrpcTransport _transport;
    private readonly WolverineOptions _options;

    internal GrpcTransportExpression(GrpcTransport transport, WolverineOptions options)
    {
        _transport = transport;
        _options = options;
    }

    /// <summary>
    /// Configures Wolverine to listen for incoming gRPC messages on the specified port.
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
    /// Configures Wolverine to send messages to the specified remote gRPC endpoint.
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
}

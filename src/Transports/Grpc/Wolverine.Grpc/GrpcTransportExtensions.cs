using JasperFx.Core.Reflection;
using Wolverine.Configuration;
using Wolverine.Transports.Grpc.Internal;

namespace Wolverine.Transports.Grpc;

/// <summary>
/// Extension methods for configuring the gRPC transport in Wolverine.
/// </summary>
public static class GrpcTransportExtensions
{
    /// <summary>
    /// The default port used by the Wolverine gRPC transport when none is specified.
    /// </summary>
    public const int DefaultPort = 5000;

    /// <summary>
    /// Registers the gRPC transport with Wolverine and returns a configuration expression
    /// that can be used to configure listeners and senders.
    /// </summary>
    /// <param name="options">The Wolverine options.</param>
    /// <returns>A <see cref="GrpcTransportExpression"/> for further configuration.</returns>
    public static GrpcTransportExpression UseGrpcTransport(this WolverineOptions options)
    {
        var transport = options.Transports.GetOrCreate<GrpcTransport>();
        return new GrpcTransportExpression(transport, options);
    }

    /// <summary>
    /// Configures Wolverine to listen for incoming gRPC messages on the specified port.
    /// </summary>
    /// <param name="options">The Wolverine options.</param>
    /// <param name="port">The TCP port on which to start the gRPC server.</param>
    /// <returns>A listener configuration for further customization.</returns>
    public static IListenerConfiguration ListenForGrpcMessages(this WolverineOptions options, int port = DefaultPort)
    {
        var transport = options.Transports.GetOrCreate<GrpcTransport>();
        var endpoint = transport.EndpointFor("localhost", port);
        endpoint.IsListener = true;
        return new ListenerConfiguration(endpoint);
    }

    /// <summary>
    /// Publishes messages to a remote Wolverine service via gRPC.
    /// </summary>
    /// <param name="publishing">The publishing expression.</param>
    /// <param name="host">The remote host name or IP address.</param>
    /// <param name="port">The remote TCP port.</param>
    /// <returns>A subscriber configuration for further customization.</returns>
    public static ISubscriberConfiguration ToGrpcEndpoint(
        this IPublishToExpression publishing,
        string host,
        int port = DefaultPort)
    {
        var uri = GrpcEndpoint.ToUri(host, port);
        publishing.As<PublishingExpression>().Parent.Transports.GetOrCreate<GrpcTransport>();
        return publishing.To(uri);
    }
}

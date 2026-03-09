using JasperFx.Core.Reflection;
using System.Security.Cryptography.X509Certificates;
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
    /// Registers both the plain (<c>grpc://</c>) and TLS (<c>grpcs://</c>) gRPC transports
    /// with Wolverine and returns a configuration expression for further setup.
    /// </summary>
    /// <param name="options">The Wolverine options.</param>
    /// <returns>A <see cref="GrpcTransportExpression"/> for further configuration.</returns>
    public static GrpcTransportExpression UseGrpcTransport(this WolverineOptions options)
    {
        var transport = options.Transports.GetOrCreate<GrpcTransport>();
        // Also ensure the secure transport is registered so grpcs:// URIs resolve correctly.
        options.Transports.GetOrCreate<GrpcSecureTransport>();
        return new GrpcTransportExpression(transport, options);
    }

    /// <summary>
    /// Configures Wolverine to listen for incoming gRPC messages on the specified port
    /// using plain HTTP/2 (no TLS).
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
    /// Configures Wolverine to listen for incoming gRPC messages on the specified port
    /// using TLS (HTTPS / <c>grpcs://</c> scheme).
    /// </summary>
    /// <param name="options">The Wolverine options.</param>
    /// <param name="port">The TCP port on which to start the secure gRPC server.</param>
    /// <param name="certificate">
    /// An optional X.509 certificate for Kestrel. When <c>null</c>, the ASP.NET Core
    /// HTTPS development certificate is used (suitable for development environments).
    /// </param>
    /// <returns>A listener configuration for further customization.</returns>
    public static IListenerConfiguration ListenForSecureGrpcMessages(
        this WolverineOptions options,
        int port = DefaultPort,
        X509Certificate2? certificate = null)
    {
        // Ensure both transports are registered
        options.Transports.GetOrCreate<GrpcTransport>();
        var secureTransport = options.Transports.GetOrCreate<GrpcSecureTransport>();

        var endpoint = secureTransport.EndpointFor("localhost", port);
        endpoint.IsListener = true;
        endpoint.TlsCertificate = certificate;
        return new ListenerConfiguration(endpoint);
    }

    /// <summary>
    /// Publishes messages to a remote Wolverine service via plain gRPC (HTTP/2, no TLS).
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

    /// <summary>
    /// Publishes messages to a remote Wolverine service via TLS-secured gRPC
    /// (<c>grpcs://</c> scheme, HTTP/2 over HTTPS).
    /// </summary>
    /// <param name="publishing">The publishing expression.</param>
    /// <param name="host">The remote host name or IP address.</param>
    /// <param name="port">The remote TCP port.</param>
    /// <returns>A subscriber configuration for further customization.</returns>
    public static ISubscriberConfiguration ToSecureGrpcEndpoint(
        this IPublishToExpression publishing,
        string host,
        int port = DefaultPort)
    {
        var uri = GrpcEndpoint.ToSecureUri(host, port);
        // Ensure both transports are registered so the secure URI resolves
        publishing.As<PublishingExpression>().Parent.Transports.GetOrCreate<GrpcTransport>();
        publishing.As<PublishingExpression>().Parent.Transports.GetOrCreate<GrpcSecureTransport>();
        return publishing.To(uri);
    }
}

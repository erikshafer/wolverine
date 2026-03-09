namespace Wolverine.Transports.Grpc.Internal;

/// <summary>
/// Handles the <c>grpcs://</c> URI scheme — a TLS-enabled variant of the gRPC transport.
/// Registered in the Wolverine <see cref="Wolverine.TransportCollection"/> alongside
/// <see cref="GrpcTransport"/> so that both <c>grpc://</c> and <c>grpcs://</c> URIs are routed
/// to the correct transport.
/// </summary>
public class GrpcSecureTransport : GrpcTransport
{
    /// <summary>Protocol name used for the <c>grpcs://</c> URI scheme.</summary>
    public new const string ProtocolName = "grpcs";

    public GrpcSecureTransport() : base(ProtocolName) { }

    /// <summary>
    /// Creates (or retrieves from cache) a <see cref="GrpcEndpoint"/> keyed on a
    /// <c>grpcs://</c> URI so that <see cref="GrpcEndpoint.IsTls"/> is always <c>true</c>
    /// for endpoints managed by this transport.
    /// </summary>
    public override GrpcEndpoint EndpointFor(string host, int port)
    {
        var uri = GrpcEndpoint.ToSecureUri(host, port);
        return (GrpcEndpoint)GetOrCreateEndpoint(uri);
    }
}

using JasperFx.Core;
using Wolverine.Configuration.Capabilities;
using Wolverine.Runtime;
using Wolverine.Transports;

namespace Wolverine.Transports.Grpc.Internal;

/// <summary>
/// The gRPC transport implementation for Wolverine. This is a non-durable, direct
/// service-to-service transport using HTTP/2 and protobuf framing.
/// </summary>
public class GrpcTransport : TransportBase<GrpcEndpoint>
{
    public const string ProtocolName = "grpc";

    private readonly LightweightCache<Uri, GrpcEndpoint> _endpoints;

    public GrpcTransport() : this(ProtocolName) { }

    /// <summary>
    /// Protected constructor that allows subclasses to register under a different
    /// URI scheme (e.g. <c>grpcs://</c> for TLS connections).
    /// </summary>
    protected GrpcTransport(string protocolName) : base(protocolName, "gRPC Transport", [protocolName])
    {
        _endpoints = new LightweightCache<Uri, GrpcEndpoint>(uri => new GrpcEndpoint(uri, this));
    }

    protected override IEnumerable<GrpcEndpoint> endpoints() => _endpoints;

    protected override GrpcEndpoint findEndpointByUri(Uri uri) => _endpoints[uri];

    public override ValueTask InitializeAsync(IWolverineRuntime runtime)
    {
        foreach (var endpoint in _endpoints)
        {
            endpoint.Compile(runtime);
        }

        return ValueTask.CompletedTask;
    }

    public override bool TryBuildBrokerUsage(out BrokerDescription description)
    {
        description = default!;
        return false;
    }

    public virtual GrpcEndpoint EndpointFor(string host, int port)
    {
        var uri = GrpcEndpoint.ToUri(host, port);
        return _endpoints[uri];
    }
}

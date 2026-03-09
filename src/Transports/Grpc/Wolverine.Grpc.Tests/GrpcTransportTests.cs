using Shouldly;
using Wolverine.Transports.Grpc;
using Wolverine.Transports.Grpc.Internal;
using Xunit;

namespace Wolverine.Grpc.Tests;

public class GrpcEndpointTests
{
    [Theory]
    [InlineData("localhost", 5005, "grpc://localhost:5005/")]
    [InlineData("my-service", 9090, "grpc://my-service:9090/")]
    public void to_uri_builds_correct_scheme(string host, int port, string expected)
    {
        var uri = GrpcEndpoint.ToUri(host, port);
        uri.ToString().ShouldBe(expected);
        uri.Scheme.ShouldBe("grpc");
        uri.Host.ShouldBe(host);
        uri.Port.ShouldBe(port);
    }

    [Fact]
    public void endpoint_uses_uri_host_and_port()
    {
        var transport = new GrpcTransport();
        var uri = GrpcEndpoint.ToUri("remote-host", 5555);
        var endpoint = new GrpcEndpoint(uri, transport);

        endpoint.HostName.ShouldBe("remote-host");
        endpoint.Port.ShouldBe(5555);
    }

    [Fact]
    public void endpoint_name_is_set_from_uri()
    {
        var transport = new GrpcTransport();
        var uri = GrpcEndpoint.ToUri("localhost", 5005);
        var endpoint = new GrpcEndpoint(uri, transport);

        endpoint.EndpointName.ShouldBe(uri.ToString());
    }
}

public class GrpcTransportTests
{
    [Fact]
    public void protocol_name_is_grpc()
    {
        var transport = new GrpcTransport();
        transport.Protocol.ShouldBe("grpc");
    }

    [Fact]
    public void transport_name_is_descriptive()
    {
        var transport = new GrpcTransport();
        transport.Name.ShouldBe("gRPC Transport");
    }

    [Fact]
    public void try_build_broker_usage_returns_false()
    {
        var transport = new GrpcTransport();
        transport.TryBuildBrokerUsage(out _).ShouldBeFalse();
    }

    [Fact]
    public void endpoint_for_returns_same_instance_for_same_host_port()
    {
        var transport = new GrpcTransport();
        var ep1 = transport.EndpointFor("localhost", 5000);
        var ep2 = transport.EndpointFor("localhost", 5000);

        ep1.ShouldBeSameAs(ep2);
    }

    [Fact]
    public void endpoint_for_returns_different_instances_for_different_ports()
    {
        var transport = new GrpcTransport();
        var ep1 = transport.EndpointFor("localhost", 5000);
        var ep2 = transport.EndpointFor("localhost", 5001);

        ep1.ShouldNotBeSameAs(ep2);
    }
}

public class GrpcTransportExtensionsTests
{
    [Fact]
    public void default_port_constant_is_5000()
    {
        GrpcTransportExtensions.DefaultPort.ShouldBe(5000);
    }

    [Fact]
    public void use_grpc_transport_registers_transport()
    {
        var options = new WolverineOptions();
        var expression = options.UseGrpcTransport();

        expression.ShouldNotBeNull();

        var transport = options.Transports.GetOrCreate<GrpcTransport>();
        transport.ShouldNotBeNull();
        transport.Protocol.ShouldBe("grpc");
    }

    [Fact]
    public void use_grpc_transport_is_idempotent()
    {
        var options = new WolverineOptions();
        var expr1 = options.UseGrpcTransport();
        var expr2 = options.UseGrpcTransport();

        // Should only register one instance
        var transports = options.Transports.OfType<GrpcTransport>().ToList();
        transports.Count.ShouldBe(1);
    }
}

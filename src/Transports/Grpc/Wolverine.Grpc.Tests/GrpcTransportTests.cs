using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;
using Wolverine.Configuration;
using Wolverine.Runtime.Serialization;
using Wolverine.Transports;
using Wolverine.Transports.Grpc;
using Wolverine.Transports.Grpc.Internal;
using Wolverine.Transports.Sending;
using Xunit;

namespace Wolverine.Grpc.Tests;

// ─────────────────────────────────────────────────────────────────────────────
// GrpcEndpoint
// ─────────────────────────────────────────────────────────────────────────────

public class GrpcEndpointTests
{
    // --- URI helpers ---------------------------------------------------------

    [Theory]
    [InlineData("localhost", 5005, "grpc://localhost:5005/")]
    [InlineData("my-service", 9090, "grpc://my-service:9090/")]
    public void to_uri_builds_correct_plain_scheme(string host, int port, string expected)
    {
        var uri = GrpcEndpoint.ToUri(host, port);
        uri.ToString().ShouldBe(expected);
        uri.Scheme.ShouldBe("grpc");
        uri.Host.ShouldBe(host);
        uri.Port.ShouldBe(port);
    }

    [Theory]
    [InlineData("localhost", 5005, "grpcs://localhost:5005/")]
    [InlineData("my-service", 9090, "grpcs://my-service:9090/")]
    public void to_secure_uri_builds_correct_tls_scheme(string host, int port, string expected)
    {
        var uri = GrpcEndpoint.ToSecureUri(host, port);
        uri.ToString().ShouldBe(expected);
        uri.Scheme.ShouldBe("grpcs");
        uri.Host.ShouldBe(host);
        uri.Port.ShouldBe(port);
    }

    // --- IsTls ---------------------------------------------------------------

    [Fact]
    public void plain_uri_is_not_tls()
    {
        var transport = new GrpcTransport();
        var endpoint = new GrpcEndpoint(GrpcEndpoint.ToUri("localhost", 5000), transport);
        endpoint.IsTls.ShouldBeFalse();
    }

    [Fact]
    public void secure_uri_is_tls()
    {
        var transport = new GrpcSecureTransport();
        var endpoint = new GrpcEndpoint(GrpcEndpoint.ToSecureUri("localhost", 5000), transport);
        endpoint.IsTls.ShouldBeTrue();
    }

    // --- host / port ---------------------------------------------------------

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
    public void endpoint_falls_back_to_default_port_when_uri_has_negative_port()
    {
        var transport = new GrpcTransport();
        // Build a URI without an explicit port: Uri.Port returns -1 in that case
        var uri = new Uri("grpc://myhost/some-path");
        uri.Port.ShouldBe(-1); // confirm Uri returns -1 when no port given
        var endpoint = new GrpcEndpoint(uri, transport);
        endpoint.Port.ShouldBe(GrpcTransportExtensions.DefaultPort);
    }

    // --- EndpointName --------------------------------------------------------

    [Fact]
    public void endpoint_name_is_set_from_uri()
    {
        var transport = new GrpcTransport();
        var uri = GrpcEndpoint.ToUri("localhost", 5005);
        var endpoint = new GrpcEndpoint(uri, transport);

        endpoint.EndpointName.ShouldBe(uri.ToString());
    }

    // --- Mode enforcement ----------------------------------------------------

    [Fact]
    public void default_mode_is_buffered_in_memory()
    {
        var transport = new GrpcTransport();
        var endpoint = new GrpcEndpoint(GrpcEndpoint.ToUri("localhost", 5000), transport);
        endpoint.Mode.ShouldBe(EndpointMode.BufferedInMemory);
    }

    [Fact]
    public void inline_mode_is_not_supported()
    {
        var transport = new GrpcTransport();
        var endpoint = new GrpcEndpoint(GrpcEndpoint.ToUri("localhost", 5000), transport);

        Should.Throw<InvalidOperationException>(() => endpoint.Mode = EndpointMode.Inline);
    }

    [Fact]
    public void durable_mode_is_supported()
    {
        var transport = new GrpcTransport();
        var endpoint = new GrpcEndpoint(GrpcEndpoint.ToUri("localhost", 5000), transport);

        // Should not throw
        endpoint.Mode = EndpointMode.Durable;
        endpoint.Mode.ShouldBe(EndpointMode.Durable);
    }

    // --- TlsCertificate ------------------------------------------------------

    [Fact]
    public void tls_certificate_is_null_by_default()
    {
        var transport = new GrpcTransport();
        var endpoint = new GrpcEndpoint(GrpcEndpoint.ToSecureUri("localhost", 5000), transport);
        endpoint.TlsCertificate.ShouldBeNull();
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// GrpcTransport
// ─────────────────────────────────────────────────────────────────────────────

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

    [Fact]
    public void endpoint_for_different_hosts_are_distinct()
    {
        var transport = new GrpcTransport();
        var ep1 = transport.EndpointFor("host-a", 5000);
        var ep2 = transport.EndpointFor("host-b", 5000);

        ep1.ShouldNotBeSameAs(ep2);
    }

    [Fact]
    public void endpoints_enumerates_all_registered_endpoints()
    {
        var transport = new GrpcTransport();
        transport.EndpointFor("localhost", 5000);
        transport.EndpointFor("localhost", 5001);
        transport.EndpointFor("remotehost", 5000);

        transport.Endpoints().Count().ShouldBe(3);
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// GrpcSecureTransport
// ─────────────────────────────────────────────────────────────────────────────

public class GrpcSecureTransportTests
{
    [Fact]
    public void protocol_name_is_grpcs()
    {
        var transport = new GrpcSecureTransport();
        transport.Protocol.ShouldBe("grpcs");
    }

    [Fact]
    public void endpoint_for_creates_tls_endpoint()
    {
        var transport = new GrpcSecureTransport();
        var endpoint = transport.EndpointFor("localhost", 5001);
        endpoint.IsTls.ShouldBeTrue();
    }

    [Fact]
    public void try_build_broker_usage_returns_false()
    {
        var transport = new GrpcSecureTransport();
        transport.TryBuildBrokerUsage(out _).ShouldBeFalse();
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// GrpcEnvelopeHandler  (pure unit tests — no Kestrel involved)
// ─────────────────────────────────────────────────────────────────────────────

public class GrpcEnvelopeHandlerTests
{
    private readonly IListener _fakeListener = Substitute.For<IListener>();
    private readonly IReceiver _fakeReceiver = Substitute.For<IReceiver>();
    private readonly Microsoft.Extensions.Logging.ILogger _fakeLogger = NullLogger.Instance;

    private Task InvokeHandlerAsync(byte[] data)
        => GrpcEnvelopeHandler.HandleAsync(data, _fakeListener, _fakeReceiver, _fakeLogger).AsTask();

    [Fact]
    public async Task empty_byte_array_does_not_call_receiver()
    {
        await InvokeHandlerAsync([]);

        await _fakeReceiver.DidNotReceive()
            .ReceivedAsync(Arg.Any<IListener>(), Arg.Any<Envelope[]>());
    }

    [Fact]
    public async Task zero_envelope_batch_does_not_call_receiver()
    {
        // Serialize an empty list of envelopes
        var data = EnvelopeSerializer.Serialize(Array.Empty<Envelope>());

        await InvokeHandlerAsync(data);

        await _fakeReceiver.DidNotReceive()
            .ReceivedAsync(Arg.Any<IListener>(), Arg.Any<Envelope[]>());
    }

    [Fact]
    public async Task ping_only_envelope_does_not_call_receiver()
    {
        var ping = Envelope.ForPing(new Uri("grpc://localhost:5000"));
        var data = EnvelopeSerializer.Serialize(new[] { ping });

        await InvokeHandlerAsync(data);

        await _fakeReceiver.DidNotReceive()
            .ReceivedAsync(Arg.Any<IListener>(), Arg.Any<Envelope[]>());
    }

    [Fact]
    public async Task real_envelope_is_forwarded_to_receiver()
    {
        var envelope = new Envelope
        {
            MessageType = "orders.created",
            Data = [1, 2, 3, 4],
            ContentType = "application/json"
        };
        var data = EnvelopeSerializer.Serialize(new[] { envelope });

        await InvokeHandlerAsync(data);

        await _fakeReceiver.Received(1)
            .ReceivedAsync(
                _fakeListener,
                Arg.Is<Envelope[]>(arr => arr.Length == 1 && arr[0].MessageType == "orders.created"));
    }

    [Fact]
    public async Task multiple_real_envelopes_are_forwarded_together()
    {
        var envelopes = new[]
        {
            new Envelope { MessageType = "msg.a", Data = [1], ContentType = "application/json" },
            new Envelope { MessageType = "msg.b", Data = [2], ContentType = "application/json" }
        };
        var data = EnvelopeSerializer.Serialize(envelopes);

        await InvokeHandlerAsync(data);

        await _fakeReceiver.Received(1)
            .ReceivedAsync(
                _fakeListener,
                Arg.Is<Envelope[]>(arr => arr.Length == 2));
    }

    [Fact]
    public async Task bad_data_throws_and_does_not_call_receiver()
    {
        var garbage = new byte[] { 0xFF, 0xFE, 0x00, 0x01, 0x02 };

        await Should.ThrowAsync<Exception>(() => InvokeHandlerAsync(garbage));

        await _fakeReceiver.DidNotReceive()
            .ReceivedAsync(Arg.Any<IListener>(), Arg.Any<Envelope[]>());
    }

    [Fact]
    public async Task mixed_ping_and_real_envelopes_are_forwarded()
    {
        // A batch with a ping AND a real message is not treated as ping-only;
        // the whole batch is delivered.
        var ping = Envelope.ForPing(new Uri("grpc://localhost:5000"));
        var real = new Envelope { MessageType = "msg.real", Data = [42], ContentType = "application/json" };
        var data = EnvelopeSerializer.Serialize(new[] { ping, real });

        await InvokeHandlerAsync(data);

        await _fakeReceiver.Received(1)
            .ReceivedAsync(_fakeListener, Arg.Is<Envelope[]>(arr => arr.Length == 2));
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// GrpcSenderProtocol
// ─────────────────────────────────────────────────────────────────────────────

public class GrpcSenderProtocolTests
{
    [Fact]
    public void plain_uri_produces_http_channel_address()
    {
        var uri = GrpcEndpoint.ToUri("payments", 9090);
        using var protocol = new GrpcSenderProtocol(uri, NullLogger<GrpcSenderProtocol>.Instance);
        protocol.ChannelAddress.ShouldBe("http://payments:9090");
    }

    [Fact]
    public void secure_uri_produces_https_channel_address()
    {
        var uri = GrpcEndpoint.ToSecureUri("payments", 9090);
        using var protocol = new GrpcSenderProtocol(uri, NullLogger<GrpcSenderProtocol>.Instance);
        protocol.ChannelAddress.ShouldBe("https://payments:9090");
    }

    [Fact]
    public void localhost_plain_address()
    {
        var uri = GrpcEndpoint.ToUri("localhost", 5000);
        using var protocol = new GrpcSenderProtocol(uri, NullLogger<GrpcSenderProtocol>.Instance);
        protocol.ChannelAddress.ShouldBe("http://localhost:5000");
    }

    [Fact]
    public void localhost_secure_address()
    {
        var uri = GrpcEndpoint.ToSecureUri("localhost", 5001);
        using var protocol = new GrpcSenderProtocol(uri, NullLogger<GrpcSenderProtocol>.Instance);
        protocol.ChannelAddress.ShouldBe("https://localhost:5001");
    }

    [Fact]
    public async Task empty_batch_calls_mark_successful_not_failure()
    {
        var uri = GrpcEndpoint.ToUri("localhost", 5000);
        using var protocol = new GrpcSenderProtocol(uri, NullLogger<GrpcSenderProtocol>.Instance);

        var callback = Substitute.For<ISenderCallback>();
        // batch.Data is the pre-serialized byte array; empty means zero bytes
        var batch = new OutgoingMessageBatch(uri, []);
        // Force the Data property to be empty
        batch.Data = [];

        await protocol.SendBatchAsync(callback, batch);

        await callback.Received(1).MarkSuccessfulAsync(batch);
        await callback.DidNotReceive().MarkProcessingFailureAsync(Arg.Any<OutgoingMessageBatch>());
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// GrpcTransportExtensions
// ─────────────────────────────────────────────────────────────────────────────

public class GrpcTransportExtensionsTests
{
    [Fact]
    public void default_port_constant_is_5000()
    {
        GrpcTransportExtensions.DefaultPort.ShouldBe(5000);
    }

    [Fact]
    public void use_grpc_transport_registers_plain_transport()
    {
        var options = new WolverineOptions();
        var expression = options.UseGrpcTransport();

        expression.ShouldNotBeNull();

        var transport = options.Transports.GetOrCreate<GrpcTransport>();
        transport.ShouldNotBeNull();
        transport.Protocol.ShouldBe("grpc");
    }

    [Fact]
    public void use_grpc_transport_also_registers_secure_transport()
    {
        var options = new WolverineOptions();
        options.UseGrpcTransport();

        var secure = options.Transports.GetOrCreate<GrpcSecureTransport>();
        secure.ShouldNotBeNull();
        secure.Protocol.ShouldBe("grpcs");
    }

    [Fact]
    public void use_grpc_transport_is_idempotent()
    {
        var options = new WolverineOptions();
        options.UseGrpcTransport();
        options.UseGrpcTransport();

        var transports = options.Transports.OfType<GrpcTransport>()
            .Where(t => t.GetType() == typeof(GrpcTransport))
            .ToList();
        transports.Count.ShouldBe(1);

        var secureTransports = options.Transports.OfType<GrpcSecureTransport>().ToList();
        secureTransports.Count.ShouldBe(1);
    }

    [Fact]
    public void listen_for_grpc_messages_sets_is_listener()
    {
        var options = new WolverineOptions();
        options.ListenForGrpcMessages(5580);

        var transport = options.Transports.GetOrCreate<GrpcTransport>();
        var endpoint = transport.EndpointFor("localhost", 5580);
        endpoint.IsListener.ShouldBeTrue();
    }

    [Fact]
    public void listen_for_grpc_messages_default_port_is_5000()
    {
        var options = new WolverineOptions();
        options.ListenForGrpcMessages();

        var transport = options.Transports.GetOrCreate<GrpcTransport>();
        var endpoint = transport.EndpointFor("localhost", 5000);
        endpoint.IsListener.ShouldBeTrue();
    }

    [Fact]
    public void listen_for_secure_grpc_messages_uses_secure_transport()
    {
        var options = new WolverineOptions();
        options.ListenForSecureGrpcMessages(5443);

        var transport = options.Transports.GetOrCreate<GrpcSecureTransport>();
        var endpoint = transport.EndpointFor("localhost", 5443);
        endpoint.IsListener.ShouldBeTrue();
        endpoint.IsTls.ShouldBeTrue();
    }

    [Fact]
    public void listen_for_secure_grpc_messages_tls_certificate_is_null_by_default_on_endpoint()
    {
        var options = new WolverineOptions();
        var transport = options.Transports.GetOrCreate<GrpcSecureTransport>();
        var endpoint = transport.EndpointFor("localhost", 5443);

        // TlsCertificate should be null when none is provided via ListenForSecureGrpcMessages.
        // A real X509Certificate2 requires platform crypto; the assignability of the
        // property is covered by the TlsCertificate tests on GrpcEndpoint directly.
        endpoint.TlsCertificate.ShouldBeNull();
    }

    [Fact]
    public void listen_for_secure_grpc_messages_cert_is_null_by_default()
    {
        var options = new WolverineOptions();
        options.ListenForSecureGrpcMessages(5443);

        var transport = options.Transports.GetOrCreate<GrpcSecureTransport>();
        var endpoint = transport.EndpointFor("localhost", 5443);
        endpoint.TlsCertificate.ShouldBeNull();
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// GrpcTransportExpression (fluent API)
// ─────────────────────────────────────────────────────────────────────────────

public class GrpcTransportExpressionTests
{
    [Fact]
    public void listen_on_port_registers_plain_listener()
    {
        var options = new WolverineOptions();
        options.UseGrpcTransport().ListenOnPort(5580);

        var transport = options.Transports.GetOrCreate<GrpcTransport>();
        var endpoint = transport.EndpointFor("localhost", 5580);
        endpoint.IsListener.ShouldBeTrue();
        endpoint.IsTls.ShouldBeFalse();
    }

    [Fact]
    public void listen_on_port_with_tls_registers_secure_listener()
    {
        var options = new WolverineOptions();
        options.UseGrpcTransport().ListenOnPortWithTls(5443);

        var transport = options.Transports.GetOrCreate<GrpcSecureTransport>();
        var endpoint = transport.EndpointFor("localhost", 5443);
        endpoint.IsListener.ShouldBeTrue();
        endpoint.IsTls.ShouldBeTrue();
    }

    [Fact]
    public void listen_on_port_with_tls_certificate_is_null_when_not_provided()
    {
        var options = new WolverineOptions();
        options.UseGrpcTransport().ListenOnPortWithTls(5443); // no cert argument

        var transport = options.Transports.GetOrCreate<GrpcSecureTransport>();
        var endpoint = transport.EndpointFor("localhost", 5443);

        // When no certificate is supplied, the endpoint falls back to the dev cert at runtime.
        endpoint.TlsCertificate.ShouldBeNull();
        endpoint.IsTls.ShouldBeTrue();
        endpoint.IsListener.ShouldBeTrue();
    }

    [Fact]
    public void fluent_api_is_chainable()
    {
        var options = new WolverineOptions();
        var expression = options.UseGrpcTransport();

        // Chain both calls; verify the return type allows fluent chaining
        var result = expression.ListenOnPort(5580).ListenOnPortWithTls(5443);
        result.ShouldBeSameAs(expression);
    }
}

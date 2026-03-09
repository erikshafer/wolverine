# Using the gRPC Transport <Badge type="tip" text="5.18" />

::: info
The gRPC transport is currently a **proof-of-concept / spike** implementation intended to validate the
design of bringing high-performance, direct service-to-service messaging into the Wolverine ecosystem. It is
**non-durable** — messages in-flight at shutdown are not persisted and will be lost. This is intentional and
by design, matching the same trade-offs as the built-in TCP transport.

If you need guaranteed delivery, durability, or fan-out, use a broker transport such as Rabbit MQ, Azure
Service Bus, or Amazon SQS instead.
:::

::: tip
The `PingPongWithGrpc` sample in the Wolverine repository is a runnable end-to-end demonstration of the gRPC
transport. Start the `Ponger` project first, then the `Pinger` project and you will see bidirectional
Ping ↔ Pong messaging over gRPC in your console output.
:::

[gRPC](https://grpc.io/) is a high-performance, open-source remote procedure call (RPC) framework originally
developed at Google. It uses **HTTP/2** for transport and **Protocol Buffers** for serialization, making it
an excellent fit for low-latency, high-throughput service-to-service communication where you want hardened
schema contracts between services.

The `WolverineFx.Grpc` package wraps Wolverine's existing binary envelope format inside a thin protobuf
message, allowing all of Wolverine's conventions — handler discovery, request/reply, correlation IDs, message
routing — to work identically to any other transport.

## Installing

Add the `WolverineFx.Grpc` NuGet package to your project:

```bash
dotnet add package WolverineFx.Grpc
```

This package depends on:

| Dependency | Purpose |
|-----------|---------|
| `Grpc.AspNetCore` | Hosts the gRPC server inside a Kestrel process (listener side) |
| `Grpc.Net.Client` | Creates managed gRPC channels for sending (sender side) |
| `Google.Protobuf` | Serializes the `EnvelopeRequest` protobuf message |

## How it works

The transport defines a minimal protobuf service:

```proto
service WolverineGrpc {
    rpc Send(EnvelopeRequest) returns (Ack);
    rpc Ping(PingRequest)     returns (Ack);
}

message EnvelopeRequest {
    bytes data = 1;  // Wolverine's existing binary-serialized Envelope batch
}
```

The `data` field carries the output of Wolverine's internal `EnvelopeSerializer`, which is the exact same
binary format used by the built-in TCP transport. This means:

- **No second serialization layer** — your message payloads are serialized once using whatever serializer you have configured in Wolverine (JSON, MessagePack, etc.)
- **All Wolverine metadata travels intact** — correlation IDs, tenant IDs, reply URIs, scheduled delivery times, etc.
- **Reply routing is automatic** — when the Ponger calls `context.RespondToSenderAsync()`, the reply is sent back to the `grpc://` URI stamped on the incoming envelope as its reply address

### Listener (server) side

When you configure a gRPC listener, Wolverine spins up a **dedicated Kestrel server** bound to HTTP/2 on
the configured port. This is independent of any other web server your application may be running. The gRPC
`WolverineGrpcService` receives incoming batches, deserializes the envelopes, and hands them to Wolverine's
normal handler pipeline.

### Sender (client) side

When you publish a message to a `grpc://` endpoint, a `GrpcChannel` (from `Grpc.Net.Client`) is opened to
the target host and port. The outgoing envelopes are batched using Wolverine's standard `BatchedSender`
infrastructure and sent via the generated `WolverineGrpcClient`.

## Configuration

### Listening for messages

Use `ListenForGrpcMessages()` to start a gRPC server and receive messages on the given port:

```csharp
using Wolverine;
using Wolverine.Transports.Grpc;

return await Host.CreateDefaultBuilder(args)
    .UseWolverine(opts =>
    {
        // Start a gRPC server on port 5581 to receive messages
        opts.ListenForGrpcMessages(5581);
    })
    .RunJasperFxCommands(args);
```

### Publishing messages to a remote gRPC endpoint

Use `ToGrpcEndpoint()` to route messages to another service that is listening via the gRPC transport:

```csharp
using Wolverine;
using Wolverine.Transports.Grpc;

return await Host.CreateDefaultBuilder(args)
    .UseWolverine(opts =>
    {
        // Route Ping messages to a remote service on port 5581
        opts.PublishMessage<Ping>().ToGrpcEndpoint("remote-host", 5581);

        // Or route all messages
        opts.PublishAllMessages().ToGrpcEndpoint("remote-host", 5581);
    })
    .RunJasperFxCommands(args);
```

### Fluent builder style

`UseGrpcTransport()` returns a `GrpcTransportExpression` for a more fluent configuration style:

```csharp
opts.UseGrpcTransport()
    .ListenOnPort(5581);   // start gRPC listener
```

### Default port

The default port is **5000** when none is specified. You can always override this:

```csharp
opts.ListenForGrpcMessages();           // port 5000
opts.ListenForGrpcMessages(port: 9090); // explicit port
opts.PublishAllMessages().ToGrpcEndpoint("svc-b");          // port 5000
opts.PublishAllMessages().ToGrpcEndpoint("svc-b", 9090);    // explicit port
```

## TLS / HTTPS (`grpcs://`)

The `WolverineFx.Grpc` transport has first-class TLS support via the `grpcs://` URI scheme.

### Listening with TLS

```csharp
using System.Security.Cryptography.X509Certificates;
using Wolverine;
using Wolverine.Transports.Grpc;

// Option A: Provide your own certificate (recommended for production)
var cert = X509Certificate2.CreateFromPemFile("server.crt", "server.key");
opts.ListenForSecureGrpcMessages(port: 5443, certificate: cert);

// Option B: Let Kestrel use the ASP.NET Core dev cert (development only)
opts.ListenForSecureGrpcMessages(port: 5443);
```

### Sending with TLS

```csharp
// Via ToSecureGrpcEndpoint (grpcs:// scheme)
opts.PublishMessage<Ping>().ToSecureGrpcEndpoint("remote-service", port: 5443);

// Or via the fluent builder
opts.UseGrpcTransport()
    .ListenOnPortWithTls(5443)            // listener with dev cert
    .SendToWithTls("remote-service", 5443); // sender using https://
```

::: tip Certificate trust for the sender
The gRPC channel on the sender side uses the system certificate store for validation.
If the remote service uses a self-signed or dev certificate, either:
- Trust the certificate at the OS/container level (`dotnet dev-certs https --trust`)
- Or terminate TLS at a load balancer/service mesh that presents a trusted certificate
:::

## Bidirectional ping/pong example

The most natural pattern for the gRPC transport is direct **request/reply** between two services. Here is an
abbreviated version of the `PingPongWithGrpc` sample:

**Pinger** — sends `Ping` every second, listens for `Pong` responses:

```csharp
// Pinger/Program.cs
return await Host.CreateDefaultBuilder(args)
    .UseWolverine(opts =>
    {
        // Receive Pong responses on this port
        opts.ListenForGrpcMessages(5580);

        // Send Ping messages to the Ponger
        opts.PublishMessage<Ping>().ToGrpcEndpoint("localhost", 5581);

        opts.Services.AddHostedService<Worker>();
    })
    .RunJasperFxCommands(args);
```

```csharp
// Pinger/PongHandler.cs
public class PongHandler
{
    public void Handle(Pong pong, ILogger<PongHandler> logger)
        => logger.LogInformation("Received Pong #{Number}", pong.Number);
}
```

**Ponger** — receives `Ping`, responds with `Pong`:

```csharp
// Ponger/Program.cs
return await Host.CreateDefaultBuilder(args)
    .UseWolverine(opts =>
    {
        opts.ApplicationAssembly = typeof(Program).Assembly;
        opts.ListenForGrpcMessages(5581);
    })
    .RunJasperFxCommands(args);
```

```csharp
// Ponger/PingHandler.cs
public class PingHandler
{
    public ValueTask Handle(Ping ping, ILogger<PingHandler> logger, IMessageContext context)
    {
        logger.LogInformation("Got Ping #{Number}", ping.Number);
        return context.RespondToSenderAsync(new Pong { Number = ping.Number });
    }
}
```

Reply routing is automatic — the incoming `Ping` envelope carries a `grpc://localhost:5580` reply URI, and
`RespondToSenderAsync()` sends the `Pong` back there without any additional configuration.

## Architecture diagram

```
┌──────────────────────────────────────┐      ┌──────────────────────────────────────┐
│  Service A  (Pinger)                 │      │  Service B  (Ponger)                  │
│                                      │      │                                       │
│  Worker ─► PublishAsync(Ping)        │─────►│  PingHandler.Handle(Ping)             │
│                                      │      │      └─► RespondToSenderAsync(Pong)   │
│  PongHandler.Handle(Pong) ◄──────────│◄─────│                                       │
│                                      │      │                                       │
│  gRPC listener  :5580                │      │  gRPC listener  :5581                 │
└──────────────────────────────────────┘      └──────────────────────────────────────┘
         grpc://localhost:5581 ────────────────────────────────────────────►
         ◄─────────────────────────────────────────── grpc://localhost:5580
```

## Comparison with other transports

| Feature | TCP | gRPC | Rabbit MQ |
|---------|-----|------|-----------|
| **Durable** | ❌ | ❌ | ✅ |
| **Broker required** | ❌ | ❌ | ✅ |
| **Protocol** | Custom binary | HTTP/2 + protobuf | AMQP |
| **Schema enforcement** | ❌ | ✅ (proto definition) | ❌ |
| **Firewall-friendly** | Port-by-port | ✅ (standard HTTP port) | Port-by-port |
| **Request / reply** | ✅ | ✅ | ✅ |
| **Broadcast / fan-out** | ❌ | ❌ | ✅ |
| **TLS** | ❌ (built-in) | ✅ (`grpcs://`) | ✅ |

::: tip When to choose gRPC vs. TCP
Choose the gRPC transport over the built-in TCP transport when:

- You want **schema-enforced contracts** between services (the proto service definition acts as a machine-readable interface)
- You need **HTTP/2 multiplexing** — multiple logical message streams over a single connection
- You are operating in an environment where only standard HTTP ports (80/443) are open in firewalls
- You want **interoperability** with gRPC clients in other languages (Go, Python, Java, etc.)

Stick with TCP when your services are all .NET, you only need a lightweight development/test transport, or you want zero additional dependencies.
:::

## Endpoint URI scheme

All gRPC endpoints use the `grpc://` URI scheme:

```
grpc://{host}:{port}
```

For example: `grpc://payments-service:5000`, `grpc://localhost:9090`.

::: warning TLS / HTTPS
The `grpcs://` scheme uses TLS (HTTP/2 over HTTPS). For the listener, a certificate must be
supplied via `ListenForSecureGrpcMessages(port, certificate)` or `ListenOnPortWithTls(port, certificate)`;
when no certificate is provided Wolverine falls back to the ASP.NET Core HTTPS development certificate
(`dotnet dev-certs https --trust`). For production deployments always supply a trusted certificate
or terminate TLS at a service mesh / ingress layer.
:::

## Spike / Proof-of-concept status

`WolverineFx.Grpc` is deliberately scoped as a **non-durable spike**. The following features are **not** yet
implemented and are tracked for future work:

| Feature | Status |
|---------|--------|
| TLS / `grpcs://` support | ✅ Implemented |
| Conventional routing (auto-discover endpoints) | Planned |
| Bi-directional streaming (for higher throughput) | Under consideration |
| Health check endpoint integration | Planned |
| Integration with existing ASP.NET Core host (shared Kestrel) | Under consideration |
| SendingCompliance test suite | Planned before stable release |

The transport is a great fit today for **greenfield service-to-service spikes**, **internal tooling**, and
**performance experiments** where you want Wolverine's handler conventions without a full broker.

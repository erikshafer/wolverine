# PingPong with gRPC Transport

This sample demonstrates Wolverine's **gRPC transport** (`WolverineFx.Grpc`) for high-performance, direct service-to-service messaging. It mirrors the classic `PingPong` (TCP) sample but replaces the TCP transport with gRPC over HTTP/2.

## What it shows

- **Bidirectional messaging** via the gRPC transport — Pinger sends `Ping` messages, Ponger responds with `Pong` messages, all over gRPC
- **Wolverine idioms preserved** — handlers, reply routing, and `IMessageContext.RespondToSenderAsync()` work identically to other transports
- **Non-durable transport** — gRPC is a lightweight, in-flight transport with no persistence; messages not yet delivered are lost if a service restarts (same behaviour as the TCP transport)

## Architecture

```
┌─────────────────────────────────┐         ┌─────────────────────────────────┐
│          Pinger                 │         │          Ponger                  │
│                                 │         │                                  │
│  Worker ──► PublishAsync(Ping)  │─gRPC──►│  PingHandler.Handle(Ping)        │
│                                 │         │      └─► RespondToSenderAsync()  │
│  PongHandler.Handle(Pong) ◄─────│◄─gRPC──│                                  │
└─────────────────────────────────┘         └─────────────────────────────────┘
  Listens on :5580                            Listens on :5581
```

## Running the sample

Open two terminal windows.

**Terminal 1 – start the Ponger:**
```bash
cd Ponger
dotnet run
```

**Terminal 2 – start the Pinger:**
```bash
cd Pinger
dotnet run
```

You should see log output like:
```
[Pinger] Sending Ping #1 via gRPC
[Ponger] Got Ping #1 via gRPC
[Pinger] Received Pong #1 via gRPC
```

## How the gRPC transport works

The `WolverineFx.Grpc` transport wraps Wolverine's existing binary envelope format inside a
simple protobuf message (`EnvelopeRequest { bytes data = 1; }`). This means:

1. The full Wolverine `Envelope` (headers, correlation IDs, reply address, etc.) travels intact
2. Only gRPC + protobuf libraries are needed — no broker, no infrastructure to provision
3. Reply routing works automatically: the Pong is sent back to whichever `grpc://` URI was
   stamped as the `ReplyUri` on the inbound Ping envelope

## Comparison with other transports

| Feature | TCP | RabbitMQ | gRPC |
|---------|-----|----------|------|
| Durable? | No | Yes | No |
| Broker required? | No | Yes | No |
| Binary protocol | Custom | AMQP | HTTP/2 + protobuf |
| Request/reply | ✅ | ✅ | ✅ |
| Broadcast / fanout | ❌ | ✅ | ❌ |
| Schema enforcement | ❌ | ❌ | ✅ (proto) |

## Configuration API

```csharp
// Listen for incoming plain gRPC messages
opts.ListenForGrpcMessages(port: 5581);

// Listen with TLS (grpcs://)
opts.ListenForSecureGrpcMessages(port: 5443, certificate: myCert); // explicit cert
opts.ListenForSecureGrpcMessages(port: 5443);                      // dev cert fallback

// Send messages to a remote plain gRPC endpoint
opts.PublishMessage<Ping>().ToGrpcEndpoint("remote-host", port: 5581);

// Send messages to a remote TLS gRPC endpoint
opts.PublishMessage<Ping>().ToSecureGrpcEndpoint("remote-host", port: 5443);

// Fluent builder style
opts.UseGrpcTransport()
    .ListenOnPort(5581)                    // plain listener
    .ListenOnPortWithTls(5443)             // TLS listener (dev cert)
    .SendToWithTls("remote-host", 5443);   // TLS sender
```

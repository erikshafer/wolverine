using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Net;
using Wolverine.Runtime;
using Wolverine.Runtime.Serialization;
using Wolverine.Transports;

namespace Wolverine.Transports.Grpc.Internal;

/// <summary>
/// <see cref="IListener"/> implementation for the gRPC transport.
/// Spins up a dedicated Kestrel server on the configured port to accept
/// incoming Wolverine messages via the <see cref="WolverineGrpcService"/>.
/// </summary>
public sealed class GrpcListener : IListener
{
    private readonly GrpcEndpoint _endpoint;
    private readonly IReceiver _receiver;
    private readonly ILogger<GrpcListener> _logger;
    private readonly CancellationToken _cancellation;

    private WebApplication? _app;
    private Task? _runTask;

    public Uri Address { get; }
    public IHandlerPipeline? Pipeline => _receiver.Pipeline;

    public GrpcListener(
        GrpcEndpoint endpoint,
        IReceiver receiver,
        ILogger<GrpcListener> logger,
        CancellationToken cancellation)
    {
        _endpoint = endpoint;
        _receiver = receiver;
        _logger = logger;
        _cancellation = cancellation;

        Address = endpoint.Uri;
        startServer();
    }

    private void startServer()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            Args = []
        });

        // Suppress default Kestrel/ASP.NET Core console logging so we don't
        // duplicate output from the parent host.
        builder.Logging.ClearProviders();

        builder.WebHost.ConfigureKestrel(kestrel =>
        {
            kestrel.Listen(IPAddress.Any, _endpoint.Port, listenOptions =>
            {
                listenOptions.Protocols = HttpProtocols.Http2;
            });
        });

        builder.Services.AddGrpc();

        // Register the listener so WolverineGrpcService can call back into it.
        builder.Services.AddSingleton(this);

        _app = builder.Build();
        _app.MapGrpcService<WolverineGrpcService>();

        _logger.LogInformation(
            "Starting gRPC listener on port {Port}",
            _endpoint.Port);

        _runTask = _app.StartAsync(_cancellation);
    }

    /// <summary>
    /// Called by <see cref="WolverineGrpcService"/> with the raw serialized envelope bytes.
    /// </summary>
    internal async ValueTask HandleAsync(byte[] data, CancellationToken cancellationToken = default)
    {
        Envelope[] envelopes;

        try
        {
            envelopes = EnvelopeSerializer.ReadMany(data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deserialize incoming gRPC envelope batch");
            throw;
        }

        if (envelopes.Length == 0)
        {
            return;
        }

        // A batch that contains only a ping envelope is a connectivity probe
        if (envelopes.Length == 1 && envelopes[0].IsPing())
        {
            return;
        }

        await _receiver.ReceivedAsync(this, envelopes);
    }

    public ValueTask CompleteAsync(Envelope envelope) => ValueTask.CompletedTask;

    public ValueTask DeferAsync(Envelope envelope) => ValueTask.CompletedTask;

    public Task<bool> TryRequeueAsync(Envelope envelope) => Task.FromResult(false);

    public async ValueTask StopAsync()
    {
        if (_app is not null)
        {
            _logger.LogInformation("Stopping gRPC listener on port {Port}", _endpoint.Port);
            await _app.StopAsync();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();

        if (_app is not null)
        {
            await _app.DisposeAsync();
            _app = null;
        }

        _receiver.Dispose();
    }
}

using Messages;
using Wolverine;

namespace Pinger;

// #region sample_PingPongGrpc_Worker

/// <summary>
/// A background worker that sends a Ping message to the Ponger service
/// every second via the gRPC transport.
/// </summary>
public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IServiceProvider _serviceProvider;

    public Worker(ILogger<Worker> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var pingNumber = 1;

        await using var scope = _serviceProvider.CreateAsyncScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
            _logger.LogInformation("Sending Ping #{Number} via gRPC", pingNumber);
            await bus.PublishAsync(new Ping { Number = pingNumber });
            pingNumber++;
        }
    }
}

// #endregion

using JasperFx;
using Messages;
using Pinger;
using Wolverine;
using Wolverine.Transports.Grpc;

// #region sample_BootstrappingPingerWithGrpc

return await Host.CreateDefaultBuilder(args)
    .UseWolverine(opts =>
    {
        // The Pinger listens on its own gRPC port so it can receive
        // Pong responses from the Ponger service.
        opts.ListenForGrpcMessages(5580);

        // Route all Ping messages to the Ponger's gRPC port.
        opts.PublishMessage<Ping>().ToGrpcEndpoint("localhost", 5581);

        // Register the background worker that sends Pings on a loop.
        opts.Services.AddHostedService<Worker>();
    })
    .RunJasperFxCommands(args);

// #endregion

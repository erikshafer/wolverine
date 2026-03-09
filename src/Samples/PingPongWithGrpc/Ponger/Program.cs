using JasperFx;
using Microsoft.Extensions.Hosting;
using Wolverine;
using Wolverine.Transports.Grpc;

// #region sample_BootstrappingPongerWithGrpc

return await Host.CreateDefaultBuilder(args)
    .UseWolverine(opts =>
    {
        opts.ApplicationAssembly = typeof(Program).Assembly;

        // The Ponger listens for incoming Ping messages on its gRPC port.
        opts.ListenForGrpcMessages(5581);
    })
    .RunJasperFxCommands(args);

// #endregion

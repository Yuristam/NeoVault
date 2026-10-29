using MassTransit;
using Microsoft.Extensions.Hosting;
using Notification.Worker;

var builder = Host.CreateDefaultBuilder(args);

builder.ConfigureServices(services =>
{
    services.AddMassTransit(massTransitConfig =>
    {
        massTransitConfig.AddConsumer<TransferCompletedConsumer>();
        massTransitConfig.AddConsumer<TransferFailedConsumer>();

        massTransitConfig.UsingRabbitMq((context, rabbitMqConfig) =>
        {
            rabbitMqConfig.Host("localhost", "/", rabbitHostConfig =>
            {
                rabbitHostConfig.Username("guest");
                rabbitHostConfig.Password("guest");
            });

            rabbitMqConfig.ConfigureEndpoints(context);
        });

    });
});

builder.Build().Run();

using MassTransit;
using Microsoft.EntityFrameworkCore;
using Transfers.API.Endpoints;
using Transfers.API.Persistence;
using Transfers.API.Workers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<TransfersDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("TransfersConnectionString")));
builder.Services.AddHttpClient("accounts", httpClient => httpClient.BaseAddress = new Uri("http://localhost:5103"));
builder.Services.AddProblemDetails();
builder.Services.AddHostedService<UnknownTransfersWorker>();

builder.Services.AddMassTransit(massTransitConfig =>
{
    massTransitConfig.UsingRabbitMq((context, rabbitMqConfig) =>
    {
        rabbitMqConfig.Host("localhost", "/", rabbitHostConfig =>
        {
            rabbitHostConfig.Username("guest");
            rabbitHostConfig.Password("guest");
        });
    });
});

var app = builder.Build();

app.MapTransferEndpoints();

app.Run();

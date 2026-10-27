using Microsoft.EntityFrameworkCore;
using Transfers.API.Endpoints;
using Transfers.API.Persistence;
using Transfers.API.Workers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<TransfersDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("TransfersConnectionString")));
builder.Services.AddHttpClient("accounts", c=>c.BaseAddress = new Uri("http://localhost:5103"));
builder.Services.AddProblemDetails();
builder.Services.AddHostedService<UnknownTransfersWorker>();

var app = builder.Build();

app.MapTransferEndpoints();

app.MapGet("/transfers/{id}", async (Guid id, TransfersDbContext db) =>
{
    var t = await db.Transfers.FindAsync(id);
    return t is null ? Results.NotFound() : Results.Ok(t);
});

app.Run();

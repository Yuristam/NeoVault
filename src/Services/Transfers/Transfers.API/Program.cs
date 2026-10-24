using Microsoft.EntityFrameworkCore;
using Transfers.API.Endpoints;
using Transfers.API.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<TransfersDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("TransfersConnectionString")));
builder.Services.AddHttpClient("accounts", c=>c.BaseAddress = new Uri("http://localhost:5103"));
builder.Services.AddProblemDetails();

var app = builder.Build();

app.MapTransferEndpoints();

//app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

app.Run();

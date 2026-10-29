using Accounts.API.Endpoints;
using Accounts.API.Persistence;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AccountsDbContext>(options => options.UseNpgsql(builder.Configuration.GetConnectionString("AccountsDb")));
builder.Services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect("localhost:6379"));
builder.Services.AddProblemDetails();  // for errors

var app = builder.Build();

app.MapAccountEndpoints();
app.MapSagaEndpoints();
app.MapOperationEnpoints();

app.Run();

using Accounts.API.Data;
using Accounts.API.Endpoints;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AccountsDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("AccountsDb")));
builder.Services.AddProblemDetails();  // for errors

var app = builder.Build();

app.MapAccountEndpoints();
app.MapSagaEndpoints();

app.Run();

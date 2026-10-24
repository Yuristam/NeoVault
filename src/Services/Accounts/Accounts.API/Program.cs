using Accounts.API.Endpoints;
using Accounts.API.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AccountsDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("AccountsDb")));
builder.Services.AddProblemDetails();  // for errors

var app = builder.Build();

app.MapAccountEndpoints();
app.MapSagaEndpoints();
app.MapOperationEnpoints();

app.Run();

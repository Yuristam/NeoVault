using Accounts.API.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AccountsDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("AccountsDb")));

var app = builder.Build();

app.MapGet("/accounts/{id:guid}", async (Guid id, AccountsDbContext db) =>
{
    var account = await db.Accounts.FindAsync(id);
    return account is null ? Results.NotFound() : Results.Ok(account);
});

app.Run();

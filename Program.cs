using Microsoft.EntityFrameworkCore;
using TicketDesk.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDb>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default")));

var app = builder.Build();

app.MapGet("/", () => "TicketDesk API is running");

app.Run();
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using TicketDesk.Data;
using TicketDesk.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDb>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default")));

builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

var app = builder.Build();

app.MapGet("/", () => "TicketDesk API is running");
app.MapTicketEndpoints();

app.Run();
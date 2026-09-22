using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using TicketDesk.Data;
using TicketDesk.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDb>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
        options.SwaggerEndpoint("/openapi/v1.json", "TicketDesk v1"));
}

app.MapGet("/", () => "TicketDesk API is running");
app.MapTicketEndpoints();

app.Run();
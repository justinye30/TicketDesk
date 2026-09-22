using Microsoft.EntityFrameworkCore;
using TicketDesk.Data;
using TicketDesk.Dtos;
using TicketDesk.Models;

namespace TicketDesk.Endpoints;

public static class TicketEndpoints
{
    public static void MapTicketEndpoints(this WebApplication app)
    {
        var tickets = app.MapGroup("/tickets");

        tickets.MapGet("/", async (AppDb db, Status? status, Priority? priority) =>
        {
            var query = db.Tickets.AsQueryable();
            if (status is not null) query = query.Where(t => t.Status == status);
            if (priority is not null) query = query.Where(t => t.Priority == priority);

            var results = await query.OrderByDescending(t => t.CreatedAt).ToListAsync();
            return results.Select(TicketResponse.From);
        });

        tickets.MapGet("/{id:int}", async (int id, AppDb db) =>
            await db.Tickets.FindAsync(id) is Ticket ticket
                ? Results.Ok(TicketResponse.From(ticket))
                : Results.NotFound());

        tickets.MapPost("/", async (CreateTicketRequest req, AppDb db) =>
        {
            var error = Validate(req.Title, req.Priority, Status.Open);
            if (error is not null) return Results.BadRequest(new { error });

            var ticket = new Ticket
            {
                Title = req.Title.Trim(),
                Description = req.Description ?? "",
                Priority = req.Priority
            };

            db.Tickets.Add(ticket);
            await db.SaveChangesAsync();
            return Results.Created($"/tickets/{ticket.Id}", TicketResponse.From(ticket));
        });

        tickets.MapPut("/{id:int}", async (int id, UpdateTicketRequest req, AppDb db) =>
        {
            var ticket = await db.Tickets.FindAsync(id);
            if (ticket is null) return Results.NotFound();

            var error = Validate(req.Title, req.Priority, req.Status);
            if (error is not null) return Results.BadRequest(new { error });

            ticket.Title = req.Title.Trim();
            ticket.Description = req.Description ?? "";
            ticket.Priority = req.Priority;
            ticket.Status = req.Status;
            ticket.AssignedTo = req.AssignedTo;

            await db.SaveChangesAsync();
            return Results.Ok(TicketResponse.From(ticket));
        });

        tickets.MapPost("/{id:int}/close", async (int id, AppDb db) =>
        {
            var ticket = await db.Tickets.FindAsync(id);
            if (ticket is null) return Results.NotFound();

            ticket.Status = Status.Closed;
            await db.SaveChangesAsync();
            return Results.Ok(TicketResponse.From(ticket));
        });

        tickets.MapDelete("/{id:int}", async (int id, AppDb db) =>
        {
            var ticket = await db.Tickets.FindAsync(id);
            if (ticket is null) return Results.NotFound();

            db.Tickets.Remove(ticket);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });
    }

    private static string? Validate(string? title, Priority priority, Status status)
    {
        if (string.IsNullOrWhiteSpace(title)) return "Title is required";
        if (title.Length > 200) return "Title must be 200 characters or fewer";
        if (!Enum.IsDefined(priority)) return "Invalid priority";
        if (!Enum.IsDefined(status)) return "Invalid status";
        return null;
    }
}
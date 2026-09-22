using Microsoft.EntityFrameworkCore;
using TicketDesk.Data;
using TicketDesk.Models;

namespace TicketDesk.Endpoints;

public static class TicketEndpoints
{
    public static void MapTicketEndpoints(this WebApplication app)
    {
        var tickets = app.MapGroup("/tickets");

        // GET /tickets?status=Open&priority=High
        tickets.MapGet("/", async (AppDb db, Status? status, Priority? priority) =>
        {
            var query = db.Tickets.AsQueryable();
            if (status is not null) query = query.Where(t => t.Status == status);
            if (priority is not null) query = query.Where(t => t.Priority == priority);
            return await query.OrderByDescending(t => t.CreatedAt).ToListAsync();
        });

        // GET /tickets/5
        tickets.MapGet("/{id:int}", async (int id, AppDb db) =>
            await db.Tickets.FindAsync(id) is Ticket ticket
                ? Results.Ok(ticket)
                : Results.NotFound());

        // POST /tickets
        tickets.MapPost("/", async (Ticket input, AppDb db) =>
        {
            if (string.IsNullOrWhiteSpace(input.Title))
                return Results.BadRequest("Title is required");

            db.Tickets.Add(input);
            await db.SaveChangesAsync();
            return Results.Created($"/tickets/{input.Id}", input);
        });

        // PUT /tickets/5
        tickets.MapPut("/{id:int}", async (int id, Ticket input, AppDb db) =>
        {
            var ticket = await db.Tickets.FindAsync(id);
            if (ticket is null) return Results.NotFound();
            if (string.IsNullOrWhiteSpace(input.Title))
                return Results.BadRequest("Title is required");

            ticket.Title = input.Title;
            ticket.Description = input.Description;
            ticket.Priority = input.Priority;
            ticket.Status = input.Status;
            ticket.AssignedTo = input.AssignedTo;

            await db.SaveChangesAsync();
            return Results.Ok(ticket);
        });

        // POST /tickets/5/close
        tickets.MapPost("/{id:int}/close", async (int id, AppDb db) =>
        {
            var ticket = await db.Tickets.FindAsync(id);
            if (ticket is null) return Results.NotFound();

            ticket.Status = Status.Closed;
            await db.SaveChangesAsync();
            return Results.Ok(ticket);
        });

        // DELETE /tickets/5
        tickets.MapDelete("/{id:int}", async (int id, AppDb db) =>
        {
            var ticket = await db.Tickets.FindAsync(id);
            if (ticket is null) return Results.NotFound();

            db.Tickets.Remove(ticket);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });
    }
}
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using TicketDesk.Data;
using TicketDesk.Dtos;
using TicketDesk.Models;

namespace TicketDesk.Endpoints;

public static class TicketEndpoints
{
    public static void MapTicketEndpoints(this WebApplication app)
    {
        var tickets = app.MapGroup("/tickets").WithTags("Tickets");

        tickets.MapGet("/", async (AppDb db, Status? status, Priority? priority) =>
        {
            var query = db.Tickets.AsQueryable();
            if (status is not null) query = query.Where(t => t.Status == status);
            if (priority is not null) query = query.Where(t => t.Priority == priority);

            var results = await query.OrderByDescending(t => t.CreatedAt).ToListAsync();
            return results.Select(TicketResponse.From).ToList();
        })
        .WithSummary("List tickets, optionally filtered by status and priority");

        tickets.MapGet("/{id:int}", async Task<Results<Ok<TicketResponse>, NotFound>> (int id, AppDb db) =>
            await db.Tickets.FindAsync(id) is Ticket ticket
                ? TypedResults.Ok(TicketResponse.From(ticket))
                : TypedResults.NotFound())
        .WithSummary("Get a ticket by id");

        tickets.MapPost("/", async Task<Results<Created<TicketResponse>, BadRequest<ErrorResponse>>> (CreateTicketRequest req, AppDb db) =>
        {
            var error = Validate(req.Title, req.Priority, Status.Open);
            if (error is not null) return TypedResults.BadRequest(new ErrorResponse(error));

            var ticket = new Ticket
            {
                Title = req.Title.Trim(),
                Description = req.Description ?? "",
                Priority = req.Priority
            };

            db.Tickets.Add(ticket);
            await db.SaveChangesAsync();
            return TypedResults.Created($"/tickets/{ticket.Id}", TicketResponse.From(ticket));
        })
        .WithSummary("Create a ticket");

        tickets.MapPut("/{id:int}", async Task<Results<Ok<TicketResponse>, NotFound, BadRequest<ErrorResponse>>> (int id, UpdateTicketRequest req, AppDb db) =>
        {
            var ticket = await db.Tickets.FindAsync(id);
            if (ticket is null) return TypedResults.NotFound();

            var error = Validate(req.Title, req.Priority, req.Status);
            if (error is not null) return TypedResults.BadRequest(new ErrorResponse(error));

            ticket.Title = req.Title.Trim();
            ticket.Description = req.Description ?? "";
            ticket.Priority = req.Priority;
            ticket.Status = req.Status;
            ticket.AssignedTo = req.AssignedTo;

            await db.SaveChangesAsync();
            return TypedResults.Ok(TicketResponse.From(ticket));
        })
        .WithSummary("Update a ticket");

        tickets.MapPost("/{id:int}/close", async Task<Results<Ok<TicketResponse>, NotFound>> (int id, AppDb db) =>
        {
            var ticket = await db.Tickets.FindAsync(id);
            if (ticket is null) return TypedResults.NotFound();

            ticket.Status = Status.Closed;
            await db.SaveChangesAsync();
            return TypedResults.Ok(TicketResponse.From(ticket));
        })
        .WithSummary("Close a ticket");

        tickets.MapDelete("/{id:int}", async Task<Results<NoContent, NotFound>> (int id, AppDb db) =>
        {
            var ticket = await db.Tickets.FindAsync(id);
            if (ticket is null) return TypedResults.NotFound();

            db.Tickets.Remove(ticket);
            await db.SaveChangesAsync();
            return TypedResults.NoContent();
        })
        .WithSummary("Delete a ticket");
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
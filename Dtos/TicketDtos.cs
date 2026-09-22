using TicketDesk.Models;

namespace TicketDesk.Dtos;

public record CreateTicketRequest(string Title, string? Description, Priority Priority);

public record UpdateTicketRequest(
    string Title, string? Description, Priority Priority, Status Status, string? AssignedTo);

public record TicketResponse(
    int Id, string Title, string Description, Priority Priority,
    Status Status, string? AssignedTo, DateTime CreatedAt)
{
    public static TicketResponse From(Ticket t) =>
        new(t.Id, t.Title, t.Description, t.Priority, t.Status, t.AssignedTo, t.CreatedAt);
}
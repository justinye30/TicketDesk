namespace TicketDesk.Models;

public class Ticket
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public Priority Priority { get; set; }
    public Status Status { get; set; } = Status.Open;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public enum Priority { Low, Medium, High }
public enum Status { Open, InProgress, Closed }
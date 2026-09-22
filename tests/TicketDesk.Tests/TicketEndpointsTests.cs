using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using TicketDesk.Dtos;
using TicketDesk.Models;

namespace TicketDesk.Tests;

public class TicketEndpointsTests(TestAppFactory factory) : IClassFixture<TestAppFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    // Must match the API's JSON settings, otherwise "High" can't be read back into the enum
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private async Task<TicketResponse> CreateTicketAsync(string title, Priority priority = Priority.Medium)
    {
        var response = await _client.PostAsJsonAsync(
            "/tickets", new CreateTicketRequest(title, "test", priority), Json);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TicketResponse>(Json))!;
    }

    [Fact]
    public async Task Create_ValidTicket_Returns201WithServerSetFields()
    {
        var response = await _client.PostAsJsonAsync(
            "/tickets", new CreateTicketRequest("Printer jammed", "3rd floor", Priority.High), Json);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var ticket = await response.Content.ReadFromJsonAsync<TicketResponse>(Json);
        Assert.NotNull(ticket);
        Assert.True(ticket.Id > 0);
        Assert.Equal(Status.Open, ticket.Status);
        Assert.Equal(Priority.High, ticket.Priority);
        Assert.Equal($"/tickets/{ticket.Id}", response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task Create_EmptyTitle_Returns400()
    {
        var response = await _client.PostAsJsonAsync(
            "/tickets", new CreateTicketRequest("", null, Priority.Low), Json);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>(Json);
        Assert.Equal("Title is required", error?.Error);
    }

    [Fact]
    public async Task Create_InvalidPriority_Returns400()
    {
        var response = await _client.PostAsJsonAsync(
            "/tickets", new CreateTicketRequest("Bad priority", null, (Priority)7), Json);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>(Json);
        Assert.Equal("Invalid priority", error?.Error);
    }

    [Fact]
    public async Task Create_IgnoresClientSuppliedIdAndTimestamp()
    {
        var sneaky = new { id = 999, title = "Sneaky", createdAt = "1999-01-01T00:00:00Z" };

        var response = await _client.PostAsJsonAsync("/tickets", sneaky, Json);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var ticket = await response.Content.ReadFromJsonAsync<TicketResponse>(Json);
        Assert.NotEqual(999, ticket!.Id);
        Assert.True(ticket.CreatedAt > DateTime.UtcNow.AddMinutes(-1));
    }

    [Fact]
    public async Task Get_MissingTicket_Returns404()
    {
        var response = await _client.GetAsync("/tickets/99999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task List_FilterByPriority_ReturnsOnlyMatchingTickets()
    {
        var high = await CreateTicketAsync("Server down", Priority.High);
        await CreateTicketAsync("Mouse broken", Priority.Low);

        var results = await _client.GetFromJsonAsync<List<TicketResponse>>("/tickets?priority=High", Json);

        Assert.NotNull(results);
        Assert.Contains(results, t => t.Id == high.Id);
        Assert.All(results, t => Assert.Equal(Priority.High, t.Priority));
    }

    [Fact]
    public async Task Close_ExistingTicket_SetsStatusClosed()
    {
        var ticket = await CreateTicketAsync("VPN slow");

        var response = await _client.PostAsync($"/tickets/{ticket.Id}/close", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var closed = await response.Content.ReadFromJsonAsync<TicketResponse>(Json);
        Assert.Equal(Status.Closed, closed!.Status);
    }
}
namespace OpsDashboard.Domain.Entities;

public sealed class AssistantConversation
{
    public int Id { get; set; }
    public int DatasetId { get; set; }
    public string UserMessage { get; set; } = string.Empty;
    public string AssistantResponse { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? UserId { get; set; }
    public Dataset? Dataset { get; set; }
}

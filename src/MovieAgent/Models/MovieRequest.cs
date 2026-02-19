namespace MovieAgent.Models;

public class MovieRequest
{
    public string? ConversationId { get; set; }
    public required string Description { get; set; }
    public List<ChatMessage>? History { get; set; }
}

public class ChatMessage
{
    public required string Role { get; set; }
    public required string Content { get; set; }
}

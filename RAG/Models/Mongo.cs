namespace RAG.Models;
public class ChatMessage
{
    public string Role { get; set; } = null!;
    public string Content { get; set; } = null!;
}

public class ChatConversation
{
    public string Id { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public List<ChatMessage> Messages { get; set; } = new();
}


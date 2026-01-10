namespace RAGChat.Models;

//public class DocumentChunk
//{
//    public string Content { get; set; } = string.Empty;
//    public float[] Embedding { get; set; } = Array.Empty<float>();
//}

public class DocumentChunk
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Content { get; set; } = "";
    public float[] Embedding { get; set; } = [];
    public ChunkMetadata Metadata { get; set; } = new();
}
public class ChunkMetadata
{
    public string Source { get; set; } = "knowledge"; // knowledge | correction
    public string Category { get; set; } = "general";
    public string Language { get; set; } = "ar";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int Priority { get; set; } = 1; // correction = أعلى
}
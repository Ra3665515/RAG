namespace RAG.Services;

public static class ChunkingService
{
    public static List<string> ChunkText(
        string text,
        int chunkSize = 400,
        int overlap = 50)
    {
        var words = text.Split(' ');
        var chunks = new List<string>();

        for (int i = 0; i < words.Length; i += chunkSize - overlap)
        {
            var chunk = string.Join(" ",
                words.Skip(i).Take(chunkSize));

            if (!string.IsNullOrWhiteSpace(chunk))
                chunks.Add(chunk);
        }

        return chunks;
    }
}

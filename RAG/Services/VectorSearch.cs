using RAGChat.Models;

namespace RAGChat.Services;

public class LocalVectorStore
{
    private readonly List<DocumentChunk> _chunks = new();

    public void Add(DocumentChunk chunk)
        => _chunks.Add(chunk);

    public List<(DocumentChunk Chunk, float Score)> Search(float[] query, int topK)
    {
        return _chunks
            .Select(c => (c, CosineSimilarity(query, c.Embedding)))
            .OrderByDescending(x => x.Item2)
            .Take(topK)
            .ToList();
    }

    private static float CosineSimilarity(float[] a, float[] b)
    {
        if (a.Length == 0 || b.Length == 0) return -1;

        float dot = 0, magA = 0, magB = 0;
        for (int i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            magA += a[i] * a[i];
            magB += b[i] * b[i];
        }
        return dot / ((float)Math.Sqrt(magA) * (float)Math.Sqrt(magB));
    }
}

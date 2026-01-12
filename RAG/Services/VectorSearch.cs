using RAGChat.Models;

namespace RAGChat.Services;

//public class LocalVectorStore
//{
//    private readonly List<DocumentChunk> _chunks = new();

//    public void Add(DocumentChunk chunk)
//        => _chunks.Add(chunk);

//    public List<(DocumentChunk Chunk, float Score)> Search(float[] query, int topK)
//    {
//        return _chunks
//            .Select(c => (c, CosineSimilarity(query, c.Embedding)))
//            .OrderByDescending(x => x.Item2)
//            .Take(topK)
//            .ToList();
//    }

//    private static float CosineSimilarity(float[] a, float[] b)
//    {
//        if (a.Length == 0 || b.Length == 0) return -1;

//        float dot = 0, magA = 0, magB = 0;
//        for (int i = 0; i < a.Length; i++)
//        {
//            dot += a[i] * b[i];
//            magA += a[i] * a[i];
//            magB += b[i] * b[i];
//        }
//        return dot / ((float)Math.Sqrt(magA) * (float)Math.Sqrt(magB));
//    }
//}

public class VectorResult
{
    public DocumentChunk Chunk { get; set; } = null!;
    public float Score { get; set; }
}

public class LocalVectorStore
{
    private readonly List<DocumentChunk> _docs = [];

    public void Add(IEnumerable<DocumentChunk> docs)
        => _docs.AddRange(docs);

    public IEnumerable<VectorResult> Search(
        float[] query,
        int topK,
        Func<DocumentChunk, bool>? filter = null)
    {
        var source = filter == null
            ? _docs
            : _docs.Where(filter);

        return source
            .Select(d => new VectorResult
            {
                Chunk = d,
                Score = Cosine(query, d.Embedding)
            })
            .OrderByDescending(x => x.Score)
            .Take(topK);
    }

    private static float Cosine(float[] a, float[] b)
    {
        float dot = 0, magA = 0, magB = 0;

        for (int i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            magA += a[i] * a[i];
            magB += b[i] * b[i];
        }

        return dot / (MathF.Sqrt(magA) * MathF.Sqrt(magB));
    }
}

namespace RAG.Services;

using RAGChat.Models;
using System.Text.Json;

public class EmbeddingPersistenceService
{
    private readonly string _path =
        Path.Combine("Data", "embeddings.json");

    private static readonly object _lock = new();

    public void Append(List<DocumentChunk> docs)
    {
        lock (_lock)
        {
            List<DocumentChunk> existing;

            if (File.Exists(_path))
            {
                var json = File.ReadAllText(_path);
                existing = JsonSerializer.Deserialize<List<DocumentChunk>>(json)
                           ?? new List<DocumentChunk>();
            }
            else
            {
                existing = new List<DocumentChunk>();
            }

            existing.AddRange(docs);

            File.WriteAllText(
                _path,
                JsonSerializer.Serialize(
                    existing,
                    new JsonSerializerOptions
                    {
                        WriteIndented = true
                    }
                )
            );
        }
    }

    // ✅ دي كانت ناقصة
    public void Remove(Func<DocumentChunk, bool> predicate)
    {
        lock (_lock)
        {
            if (!File.Exists(_path))
                return;

            var json = File.ReadAllText(_path);

            var existing =
                JsonSerializer.Deserialize<List<DocumentChunk>>(json)
                ?? new List<DocumentChunk>();

            existing = existing
                .Where(d => !predicate(d))
                .ToList();

            File.WriteAllText(
                _path,
                JsonSerializer.Serialize(
                    existing,
                    new JsonSerializerOptions
                    {
                        WriteIndented = true
                    }
                )
            );
        }
    }
}



using RAGChat.Models;

namespace RAG.Services;

public class AnswerCache
{
    private readonly Dictionary<string, ChatResponse> _cache = new();

    public bool TryGet(string question, out ChatResponse response)
        => _cache.TryGetValue(Normalize(question), out response);

    public void Set(string question, ChatResponse response)
        => _cache[Normalize(question)] = response;

    private static string Normalize(string q)
        => q.Trim().ToLowerInvariant();
}
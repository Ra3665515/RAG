//namespace RAG.Services;

//using OpenAI.VectorStores;
//using RAGChat.Models;
//using RAGChat.Services;
//using System.Net.Http.Headers;
//using System.Text.Json;

//public class RagService
//{
//    private readonly EmbeddingService _embedding;
//    private readonly LocalVectorStore _store;
//    private readonly HttpClient _http;
//    private readonly IConfiguration _config;

//    private readonly AnswerCache _cache;

//    private readonly List<(string Role, string Content)> _chatHistory = new();
//    private readonly EmbeddingPersistenceService _persistence;

//    public RagService(
//        EmbeddingService embedding,
//        LocalVectorStore store,
//        HttpClient http,
//        IConfiguration config, AnswerCache cache, EmbeddingPersistenceService persistence)
//    {
//        _embedding = embedding;
//        _store = store;
//        _http = http;
//        _config = config;
//        _cache = cache;
//        _persistence = persistence;

//    }

//    public async Task<ChatResponse> AskAsync(string question)
//    {
//        //  CACHE CHECK 
//        if (_cache.TryGet(question, out var cached))
//        {
//            return new ChatResponse
//            {
//                Source = "cache",
//                Answer = cached.Answer
//            };
//        }

//        _chatHistory.Add(("user", question));
//        // AUTO CATEGORY
//        var category = await DetectCategoryHybridAsync(question);
//        //  Embedding
//        var qEmbedding = (await _embedding
//            .CreateBatchEmbeddingAsync(new() { question }))
//            .First();

//        //  Vector Search
//        //var results = _store.Search(qEmbedding, 5).ToList();
//        //Vector Search + FILTERING
//        var results = _store.Search(
//        qEmbedding,
//        5,
//        d => d.Metadata.Category == category
//    )
//    .ToList();

//        var bestScore = results.Any() ? results.First().Score : 0f;

//        ChatResponse response;

//        if (bestScore < 0.55f)
//        {
//            var aiAnswer = await AskAiWithHistoryAsync();

//            response = new ChatResponse
//            {
//                Source = "ai",
//                Answer = aiAnswer
//            };

//            // ⭐ SELF LEARNING
//            await LearnFromAIAnswerAsync(
//                question,
//                aiAnswer,
//                category
//            );
//        }
//        else
//        {
//            var context = string.Join("\n\n",
//                results.Select(r => r.Chunk.Content));

//            var baseAnswer =
//                await AskWithContextAndHistoryAsync(context);

//            if (bestScore >= 0.7f)
//            {
//                baseAnswer =
//                    await ImproveWithAI(baseAnswer, question);

//                response = new ChatResponse
//                {
//                    Source = "knowledge + ai",
//                    Answer = baseAnswer
//                };
//            }
//            else
//            {
//                response = new ChatResponse
//                {
//                    Source = "data",
//                    Answer = baseAnswer
//                };
//            }
//        }

//        // SAVE TO CACHE
//        _cache.Set(question, response);

//        _chatHistory.Add(("assistant", response.Answer));

//        return response;
//    }

//    // ================= AI METHODS =================

//    private async Task<string> AskAiWithHistoryAsync()
//    {
//        var messages = new List<object>
//        {
//            new
//            {
//                role = "system",
//                content =
//                "You are a helpful assistant. " +
//                "Keep the conversation flowing naturally."
//            }
//        };

//        foreach (var (role, content) in _chatHistory.TakeLast(6))
//            messages.Add(new { role, content });

//        return await SendAsync(messages);
//    }

//    private async Task<string> AskWithContextAndHistoryAsync(
//        string context)
//    {
//        var messages = new List<object>
//        {
//            new
//            {
//                role = "system",
//                content =
//                "Answer ONLY using the provided context. " +
//                "If the answer is not found, say you don't know."
//            },
//            new
//            {
//                role = "system",
//                content = $"Context:\n{context}"
//            }
//        };

//        foreach (var (role, content) in _chatHistory.TakeLast(6))
//            messages.Add(new { role, content });

//        return await SendAsync(messages);
//    }

//    private async Task<string> ImproveWithAI(
//     string baseAnswer,
//     string question)
//    {
//        var messages = new[]
//        {
//        new
//        {
//            role = "system",
//            content =
//            "You are a senior technical advisor. " +
//            "Improve the answer for clarity, completeness, and professionalism. " +
//            "You MAY add short, useful recommendations or mention related roles " +
//            "ONLY if they are logically implied by the context. " +
//            "You MUST respect the provided context as factual and authoritative. " +

//            "Do NOT invent names, facts, or internal company information."
//        },
//        new
//        {
//            role = "user",
//            content =
//            //$"Question:\n{question}\n\n" +
//            $"Answer:\n{baseAnswer}"
//        }
//    };

//        return await SendAsync(messages, maxTokens: 180, temperature: 0.45);
//    }


//    private async Task<string> SendAsync(
//        object messages,
//        int maxTokens = 200,
//        double temperature = 0.2)
//    {
//        var body = new
//        {
//            model = _config["Model"],
//            messages,
//            max_tokens = maxTokens,
//            temperature
//        };

//        using var req = new HttpRequestMessage(
//            HttpMethod.Post,
//            "https://api.openai.com/v1/chat/completions");

//        req.Headers.Authorization =
//            new AuthenticationHeaderValue(
//                "Bearer", _config["OpenAI:ApiKey"]);

//        req.Content = JsonContent.Create(body);

//        using var res = await _http.SendAsync(req);
//        res.EnsureSuccessStatusCode();

//        using var json =
//            await res.Content.ReadFromJsonAsync<JsonDocument>();

//        return json!
//            .RootElement
//            .GetProperty("choices")[0]
//            .GetProperty("message")
//            .GetProperty("content")
//            .GetString()!;
//    }
//    private async Task<string> DetectCategoryHybridAsync(string question)
//    {
//        var category = DetectCategoryRuleBased(question);

//        if (category == "general")
//        {
//            category = await DetectCategoryWithAIAsync(question);
//        }

//        return category;
//    }

//    private async Task<string> DetectCategoryWithAIAsync(string question)
//    {
//        var messages = new[]
//        {
//        new
//        {
//            role = "system",
//            content =
//            "Classify the following question into one category only: " +
//            "dotnet, cloud, hr, general. " +
//            "Answer with ONE word only."
//        },
//        new
//        {
//            role = "user",
//            content = question
//        }
//    };

//        var result = await SendAsync(
//            messages,
//            maxTokens: 5,
//            temperature: 0);

//        return result.Trim().ToLowerInvariant();
//    }

//    public static string DetectCategoryRuleBased(string question)
//    {
//        question = question.ToLowerInvariant();

//        if (question.Contains("cloud") ||
//            question.Contains("aws") ||
//            question.Contains("azure") ||
//            question.Contains("devops"))
//            return "cloud";

//        if (question.Contains("asp.net") ||
//            question.Contains(".net") ||
//            question.Contains("c#"))
//            return "dotnet";

//        if (question.Contains("hr") ||
//            question.Contains("salary") ||
//            question.Contains("career"))
//            return "hr";

//        return "general";
//    }
//    private async Task LearnFromAIAnswerAsync(
//    string question,
//    string aiAnswer,
//    string category)
//    {
//        // 1️⃣ Chunk answer
//        var chunks = ChunkingService.ChunkText(aiAnswer, 200, 40);

//        // 2️⃣ Create embeddings
//        var embeddings = await _embedding
//            .CreateBatchEmbeddingAsync(chunks);

//        var docs = new List<DocumentChunk>();

//        for (int i = 0; i < chunks.Count; i++)
//        {
//            docs.Add(new DocumentChunk
//            {
//                Content = chunks[i],
//                Embedding = embeddings[i],
//                Metadata = new ChunkMetadata
//                {
//                    Source = "ai-learning",
//                    Category = category,
//                    Priority = 0 // أقل من human / knowledge
//                }
//            });
//        }

//        // 3️⃣ Save to vector store
//        _store.Add(docs);
//        _persistence.Append(docs);

//    }
//    private async Task LearnFromAnswerAsync(
//    string answer,
//    string category)
//    {
//        var chunks = ChunkingService.ChunkText(answer);

//        var embeddings =
//            await _embedding.CreateBatchEmbeddingAsync(chunks);

//        var docs = new List<DocumentChunk>();

//        for (int i = 0; i < chunks.Count; i++)
//        {
//            docs.Add(new DocumentChunk
//            {
//                Content = chunks[i], // الإجابة فقط
//                Embedding = embeddings[i],
//                Metadata = new ChunkMetadata
//                {
//                    Category = category,
//                    Source = "self-learned",
//                    Priority = 2
//                }
//            });
//        }

//        _store.Add(docs);
//    }


//}
namespace RAG.Services;

using RAGChat.Models;
using RAGChat.Services;
using System.Net.Http.Headers;
using System.Text.Json;

public class RagService
{
    private readonly EmbeddingService _embedding;
    private readonly LocalVectorStore _store;
    private readonly HttpClient _http;
    private readonly IConfiguration _config;
    private readonly AnswerCache _cache;
    private readonly EmbeddingPersistenceService _persistence;

    public RagService(
        EmbeddingService embedding,
        LocalVectorStore store,
        HttpClient http,
        IConfiguration config,
        AnswerCache cache,
        EmbeddingPersistenceService persistence)
    {
        _embedding = embedding;
        _store = store;
        _http = http;
        _config = config;
        _cache = cache;
        _persistence = persistence;
    }

    public async Task<ChatResponse> AskAsync(string question)
    {
        // ================= CACHE =================
        if (_cache.TryGet(question, out var cached))
            return cached;

        // ================= CATEGORY =================
        var category = await DetectCategoryHybridAsync(question);

        // ================= EMBEDDING =================
        var qEmbedding = (await _embedding
            .CreateBatchEmbeddingAsync(new() { question }))
            .First();

        // ================= VECTOR SEARCH =================
        var results = _store.Search(
                qEmbedding,
                topK: 5,
                filter: d =>
                    d.Metadata.Category == category &&
                    d.Metadata.Priority >= 1
            )
            .OrderByDescending(r => r.Chunk.Metadata.Priority)
            .ThenByDescending(r => r.Score)
            .ToList();

        var bestScore = results.Any() ? results.First().Score : 0f;

        ChatResponse response;

        // ================= AI ONLY =================
        if (bestScore < 0.55f && !results.Any())
        {
            var aiAnswer = await AskAiAsync(question);

            var improved =
                await ImproveWithAI(aiAnswer, question);

            response = new ChatResponse
            {
                Source = "ai",
                Answer = improved
            };

            // ⭐ SELF LEARNING (ANSWER ONLY)
            await LearnFromAnswerAsync(improved, category);
        }
        else
        {
            var context = string.Join(
                "\n\n",
                results.Select(r => r.Chunk.Content));

            var baseAnswer =
                await AskWithContextAsync(question, context);

            if (bestScore >= 0.7f)
            {
                baseAnswer =
                    await ImproveWithAI(baseAnswer, question);

                response = new ChatResponse
                {
                    Source = "knowledge + ai",
                    Answer = baseAnswer
                };
            }
            else
            {
                response = new ChatResponse
                {
                    Source = "data",
                    Answer = baseAnswer
                };
            }
        }

        // ================= CACHE SAVE =================
        _cache.Set(question, response);

        return response;
    }

    // ================= AI METHODS =================

    private async Task<string> AskAiAsync(string question)
    {
        var messages = new[]
        {
            new
            {
                role = "system",
                content = "You are a helpful assistant."
            },
            new
            {
                role = "user",
                content = question
            }
        };

        return await SendAsync(messages);
    }

    //private async Task<string> AskWithContextAsync(
    //    string question,
    //    string context)
    //{
    //    var messages = new[]
    //    {
    //        new
    //        {
    //            role = "system",
    //            content =
    //                "Answer ONLY using the provided context. " +
    //                "If the answer is not found, say you don't know."
    //        },
    //        new
    //        {
    //            role = "system",
    //            content = $"Context:\n{context}"
    //        },
    //        new
    //        {
    //            role = "user",
    //            content = question
    //        }
    //    };

    //    return await SendAsync(messages);
    //}
    private async Task<string> AskWithContextAsync(
    string question,
    string context)
    {
        var messages = new[]
        {
        new
        {
            role = "system",
            content =
                "You are an expert assistant. " +
                "Answer strictly using the provided context. " +
                "If the answer is not explicitly stated, infer it logically from the context. " +
                "DO NOT say 'I don't know'. " +
                "DO NOT mention the word 'context'. " +
                "Provide the most helpful and complete answer possible."
        },
        new
        {
            role = "system",
            content = $"Context:\n{context}"
        },
        new
        {
            role = "user",
            content = question
        }
    };

        return await SendAsync(
            messages,
            maxTokens: 220,
            temperature: 0.15);
    }


    private async Task<string> ImproveWithAI(
        string baseAnswer,
        string question)
    {
        var messages = new[]
        {
            new
            {
                role = "system",
                content =
                    "You are a senior technical advisor. " +
                    "Rewrite the answer to be clear, concise, and professional. " +
                    "Do NOT add new facts, names, or assumptions."
            },
            new
            {
                role = "user",
                content = baseAnswer
            }
        };

        return await SendAsync(
            messages,
            maxTokens: 180,
            temperature: 0.35);
    }

    private async Task<string> SendAsync(
        object messages,
        int maxTokens = 200,
        double temperature = 0.2)
    {
        var body = new
        {
            model = _config["Model"],
            messages,
            max_tokens = maxTokens,
            temperature
        };

        using var req = new HttpRequestMessage(
            HttpMethod.Post,
            "https://api.openai.com/v1/chat/completions");

        req.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                _config["OpenAI:ApiKey"]);

        req.Content = JsonContent.Create(body);

        using var res = await _http.SendAsync(req);
        res.EnsureSuccessStatusCode();

        using var json =
            await res.Content.ReadFromJsonAsync<JsonDocument>();

        return json!
            .RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString()!;
    }

    // ================= CATEGORY =================

    private async Task<string> DetectCategoryHybridAsync(string question)
    {
        var category = DetectCategoryRuleBased(question);

        if (category == "general")
            category = await DetectCategoryWithAIAsync(question);

        return category;
    }

    private async Task<string> DetectCategoryWithAIAsync(string question)
    {
        var messages = new[]
        {
            new
            {
                role = "system",
                content =
                    "Classify the question into ONE category only: " +
                    "dotnet, cloud, hr, general."
            },
            new
            {
                role = "user",
                content = question
            }
        };

        var result = await SendAsync(
            messages,
            maxTokens: 5,
            temperature: 0);

        result = result.Trim().ToLowerInvariant();

        return result switch
        {
            "dotnet" => "dotnet",
            "cloud" => "cloud",
            "hr" => "hr",
            _ => "general"
        };
    }

    public static string DetectCategoryRuleBased(string question)
    {
        question = question.ToLowerInvariant();

        if (question.Contains("aws") ||
            question.Contains("azure") ||
            question.Contains("cloud") ||
            question.Contains("devops"))
            return "cloud";

        if (question.Contains(".net") ||
            question.Contains("asp.net") ||
            question.Contains("c#"))
            return "dotnet";

        if (question.Contains("hr") ||
            question.Contains("salary") ||
            question.Contains("career"))
            return "hr";

        return "general";
    }

    // ================= SELF LEARNING =================

    private async Task LearnFromAnswerAsync(
        string answer,
        string category)
    {
        var chunks = ChunkingService.ChunkText(
            answer,
            chunkSize: 200,
            overlap: 40);

        var embeddings =
            await _embedding.CreateBatchEmbeddingAsync(chunks);

        var docs = new List<DocumentChunk>();

        for (int i = 0; i < chunks.Count; i++)
        {
            docs.Add(new DocumentChunk
            {
                Content = chunks[i],
                Embedding = embeddings[i],
                Metadata = new ChunkMetadata
                {
                    Source = "self-learned",
                    Category = category,
                    Priority = 1
                }
            });
        }

        _store.Add(docs);
        _persistence.Append(docs);
    }
    public async Task LearnFromHumanFeedbackAsync(
    string correctAnswer,
    string category)
    {
        var chunks = ChunkingService.ChunkText(
            correctAnswer, 200, 40);

        var embeddings =
            await _embedding.CreateBatchEmbeddingAsync(chunks);

        var docs = new List<DocumentChunk>();

        for (int i = 0; i < chunks.Count; i++)
        {
            docs.Add(new DocumentChunk
            {
                Content = chunks[i],
                Embedding = embeddings[i],
                Metadata = new ChunkMetadata
                {
                    Source = "human-feedback",
                    Category = category,
                    Priority = 3 
                }
            });
        }

        _store.Add(docs);
        _persistence.Append(docs);
    }

}

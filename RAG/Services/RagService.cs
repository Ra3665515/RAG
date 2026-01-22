namespace RAG.Services;

using RAGChat.Controllers;
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
    private ChatInteraction? _lastInteraction;

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
        {
            if (cached.Source == "human-feedback")
                return cached;
        }

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
                filter: d => d.Metadata.Category == category
            )
            .OrderByDescending(r => r.Chunk.Metadata.Priority) // priority أولاً
            .ThenByDescending(r => r.Score)
            .ToList();

        // ================= HUMAN FEEDBACK OVERRIDE =================
        var top = results.FirstOrDefault();

        if (top?.Chunk.Metadata.Source == "human-feedback")
        {
            var answer = top.Chunk.Content
                .Split("A:", 2)[1]
                .Trim();

            var response = new ChatResponse
            {
                Source = "human-feedback",
                Answer = answer
            };

            _cache.Set(question, response);

            _lastInteraction = new ChatInteraction
            {
                Question = question,
                Answer = answer,
                Category = category
            };

            return response;
        }

        // ================= OLD LOGIC (COMMENTED) =================
        /*
        var bestScore = results.Any() ? results.First().Score : 0f;

        ChatResponse response;

        if (bestScore < 0.55f)
        {
            var aiAnswer = await AskAiAsync(question);

            var improved =
                await ImproveWithAI(aiAnswer, question);

            response = new ChatResponse
            {
                Source = "ai",
                Answer = improved
            };

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
        */

        // ================= FALLBACK (NO HUMAN FEEDBACK) =================
        // نفس المنطق القديم لكن من غير خلط مع feedback

        var fallbackContext = string.Join(
            "\n\n",
            results.Select(r => r.Chunk.Content));

        var fallbackAnswer =
            await AskWithContextAsync(question, fallbackContext);

        var finalResponse = new ChatResponse
        {
            Source = "knowledge",
            Answer = fallbackAnswer
        };

        _cache.Set(question, finalResponse);

        _lastInteraction = new ChatInteraction
        {
            Question = question,
            Answer = finalResponse.Answer,
            Category = category
        };

        return finalResponse;
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

    public async Task<bool> LearnFromLastInteractionAsync(string correctAnswer)
    {
        if (_lastInteraction == null)
            return false;

        var question = _lastInteraction.Question;
        var oldAnswer = _lastInteraction.Answer;
        var category = _lastInteraction.Category;

        // 1️⃣ Validate with AI
        var isValid = await ValidateCorrectionWithAI(
            question,
            oldAnswer,
            correctAnswer);

        if (!isValid)
            return false;

        // 2️⃣ Apply feedback (embedding + cache)
        await ApplyHumanFeedback(question, correctAnswer, category);

        return true;
    }
    ////دي لو مش هنمسح القديم من الامبيدنج
    //private async Task ApplyHumanFeedback(
    //string question,
    //string correctAnswer,
    //string category)
    //{
    //    var qEmbedding = (await _embedding
    //        .CreateBatchEmbeddingAsync(new() { question }))
    //        .First();

    //    var qaText = $"Q: {question}\nA: {correctAnswer}";

    //    var doc = new DocumentChunk
    //    {
    //        Content = qaText,
    //        Embedding = qEmbedding,
    //        Metadata = new ChunkMetadata
    //        {
    //            Source = "human-feedback",
    //            Category = category,
    //            Priority = 5
    //        }
    //    };

    //    _store.Add(new[] { doc });
    //    _persistence.Append(new List<DocumentChunk> { doc });

    //    _cache.Set(question, new ChatResponse
    //    {
    //        Source = "human-feedback",
    //        Answer = correctAnswer
    //    });
    //}
    private async Task ApplyHumanFeedback(
    string question,
    string correctAnswer,
    string category)
    {
        // 1️⃣ امسحي أي إجابة قديمة للسؤال ده
        _store.Remove(d =>
            d.Content.Contains($"Q: {question}") ||
            d.Metadata.Source != "human-feedback" &&
            d.Metadata.Category == category
        );

        _persistence.Remove(d =>
            d.Content.Contains($"Q: {question}") ||
            d.Metadata.Source != "human-feedback" &&
            d.Metadata.Category == category
        );

        // 2️⃣ اعملي embedding للسؤال
        var qEmbedding = (await _embedding
            .CreateBatchEmbeddingAsync(new() { question }))
            .First();

        // 3️⃣ خزني الجديد فقط
        var doc = new DocumentChunk
        {
            Content = $"Q: {question}\nA: {correctAnswer}",
            Embedding = qEmbedding,
            Metadata = new ChunkMetadata
            {
                Source = "human-feedback",
                Category = category,
                Priority = 5
            }
        };

        _store.Add(new[] { doc });
        _persistence.Append(new List<DocumentChunk> { doc });

        // 4️⃣ حدّثي الكاش
        _cache.Set(question, new ChatResponse
        {
            Source = "human-feedback",
            Answer = correctAnswer
        });
    }


    private async Task<bool> ValidateCorrectionWithAI(
    string question,
    string oldAnswer,
    string correctedAnswer)
    {
        var messages = new[]
        {
        new
        {
            role = "system",
            content =
                "You are an expert reviewer. " +
                "Decide if the corrected answer properly and accurately answers the question. " +
                "Reply ONLY with YES or NO."
        },
        new
        {
            role = "user",
            content =
                $"Question:\n{question}\n\n" +
                $"Old Answer:\n{oldAnswer}\n\n" +
                $"Corrected Answer:\n{correctedAnswer}"
        }
    };

        var result = await SendAsync(
            messages,
            maxTokens: 5,
            temperature: 0);

        return result.Trim().Equals("yes", StringComparison.OrdinalIgnoreCase);
    }



}
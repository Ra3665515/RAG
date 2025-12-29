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

//    public RagService(
//        EmbeddingService embedding,
//        LocalVectorStore store,
//        HttpClient http,
//        IConfiguration config)
//    {
//        _embedding = embedding;
//        _store = store;
//        _http = http;
//        _config = config;
//    }

//    public async Task<ChatResponse> AskAsync(string question)
//    {
//        var embeddings = await _embedding.CreateBatchEmbeddingAsync(
//            new List<string> { question });

//        var qEmbedding = embeddings.First();
//        // 2️⃣ Search in vector store
//        var results = _store.Search(qEmbedding, 5).ToList();

//        //// 3️⃣ لو مفيش نتائج خالص → AI
//        //if (!results.Any())
//        //{
//        //    return new ChatResponse
//        //    {
//        //        Source = "ai",
//        //        Answer = await AskAiAsync(question)
//        //    };
//        //}

//        var bestScore = results.First().Score;

//        // 1️⃣ AI فقط (مفيش داتا موثوقة)
//        if (bestScore < 0.55f)
//        {
//            return new ChatResponse
//            {
//                Source = "ai",
//                Answer = await AskAiAsync(question)
//            };
//        }

//        var context = string.Join("\n\n",
//            results.Select(r => r.Chunk.Content));

//        var answerFromContext =
//            await AskWithContextAsync(question, context);

//        if (bestScore >= 0.7f)
//        {
//            var improved =
//                await ImproveWithAI(answerFromContext, question);

//            return new ChatResponse
//            {
//                Source = "knowledge + ai",
//                Answer = improved
//            };
//        }

//        return new ChatResponse
//        {
//            Source = "data",
//            Answer = answerFromContext
//        };

//    }



//    private async Task<string> AskWithContextAsync(string question, string context)
//    {
//        var messages = new[]
//        {
//            new { role = "system", content = "Answer ONLY from context." },
//            new { role = "system", content = $"Context:\n{context}" },
//            new { role = "user", content = question }
//        };

//        return await SendAsync(messages);
//    }

//    private async Task<string> AskAiAsync(string question)
//    {
//        var messages = new[]
//        {
//            new { role = "system", content = "You are a helpful assistant." },
//            new { role = "user", content = question }
//        };

//        return await SendAsync(messages);
//    }

//    //private async Task<string> SendAsync(object messages)
//    //{
//    //    var body = new
//    //    {
//    //        model = _config["Model"],
//    //        messages = messages,
//    //        max_tokens = int.Parse(_config["MaxTokens"]!),
//    //        temperature = double.Parse(_config["Temperature"]!)
//    //    };

//    //    using var req = new HttpRequestMessage(
//    //        HttpMethod.Post,
//    //        "https://api.openai.com/v1/chat/completions");

//    //    req.Headers.Authorization =
//    //        new AuthenticationHeaderValue("Bearer", _config["OpenAI:ApiKey"]);

//    //    req.Content = JsonContent.Create(body);

//    //    var res = await _http.SendAsync(req);
//    //    res.EnsureSuccessStatusCode();

//    //    using var json = await JsonDocument.ParseAsync(
//    //        await res.Content.ReadAsStreamAsync());

//    //    return json
//    //        .RootElement
//    //        .GetProperty("choices")[0]
//    //        .GetProperty("message")
//    //        .GetProperty("content")
//    //        .GetString()!;
//    //}
//    private async Task<string> SendAsync(
//    object messages,
//    int maxTokens = 200,
//    double temperature = 0.2)
//    {
//        var body = new
//        {
//            model = _config["Model"],
//            messages,
//            max_tokens = maxTokens,
//            temperature = temperature
//        };

//        using var req = new HttpRequestMessage(
//            HttpMethod.Post,
//            "https://api.openai.com/v1/chat/completions");

//        req.Headers.Authorization =
//            new AuthenticationHeaderValue("Bearer",
//                _config["OpenAI:ApiKey"]);

//        req.Content = JsonContent.Create(body);

//        using var res = await _http.SendAsync(req);
//        res.EnsureSuccessStatusCode();

//        using var json = await res.Content.ReadFromJsonAsync<JsonDocument>();

//        return json!
//            .RootElement
//            .GetProperty("choices")[0]
//            .GetProperty("message")
//            .GetProperty("content")
//            .GetString()!;
//    }

//    #region improveWithAI
//    //public async Task<ChatResponse> AskAsync(string question)
//    //{
//    //    var embeddings = await _embedding.CreateBatchEmbeddingAsync(
//    //        new List<string> { question });

//    //    var qEmbedding = embeddings.First();

//    //    var results = _store.Search(qEmbedding, 3);

//    //    var bestScore = results
//    //        .Select(r => r.Score)
//    //        .DefaultIfEmpty(0f)
//    //        .Max();

//    //    if (bestScore < 0.65f)
//    //    {
//    //        return new ChatResponse
//    //        {
//    //            Source = "ai",
//    //            Answer = await AskAiAsync(question)
//    //        };
//    //    }

//    //    var context = string.Join("\n\n",
//    //        results.Select(r => r.Chunk.Content));

//    //    var baseAnswer = await AskWithContextAsync(question, context);
//    //    var improved = await ImproveWithAI(baseAnswer, question);

//    //    return new ChatResponse
//    //    {
//    //        Source = "knowledge + ai",
//    //        Answer = improved
//    //    };
//    //}
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
//            "Rewrite the answer for clarity and professionalism. " +
//            "You MAY add helpful recommendations ONLY if they are logically implied. " +
//            "DO NOT add new facts, names, or assumptions."
//        },
//        new
//        {
//            role = "user",
//            content =
//            $"Question:\n{question}\n\n" +
//            $"Answer:\n{baseAnswer}"
//        }
//    };

//        return await SendAsync(messages, maxTokens: 150, temperature: 0.3);
//    }

//    #endregion
//}
namespace RAG.Services;

using OpenAI.VectorStores;
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

    
    private readonly List<(string Role, string Content)> _chatHistory = new();

    public RagService(
        EmbeddingService embedding,
        LocalVectorStore store,
        HttpClient http,
        IConfiguration config)
    {
        _embedding = embedding;
        _store = store;
        _http = http;
        _config = config;
    }

    public async Task<ChatResponse> AskAsync(string question)
    {
        // احفظ سؤال 
        _chatHistory.Add(("user", question));

        //  Embedding
        var qEmbedding = (await _embedding
            .CreateBatchEmbeddingAsync(new() { question }))
            .First();

        //  Vector Search
        var results = _store.Search(qEmbedding, 5).ToList();
        var bestScore = results.Any() ? results.First().Score : 0f;

        
        if (bestScore < 0.55f)
        {
            var aiAnswer = await AskAiWithHistoryAsync();

            _chatHistory.Add(("assistant", aiAnswer));

            return new ChatResponse
            {
                Source = "ai",
                Answer = aiAnswer
            };
        }

        //  Build Context
        var context = string.Join("\n\n",
            results.Select(r => r.Chunk.Content));

        var baseAnswer =
            await AskWithContextAndHistoryAsync(context);

        // Knowledge + AI 
        if (bestScore >= 0.7f)
        {
            baseAnswer =
                await ImproveWithAI(baseAnswer, question);

            _chatHistory.Add(("assistant", baseAnswer));

            return new ChatResponse
            {
                Source = "knowledge + ai",
                Answer = baseAnswer
            };
        }

        //  Data فقط
        _chatHistory.Add(("assistant", baseAnswer));

        return new ChatResponse
        {
            Source = "data",
            Answer = baseAnswer
        };
    }

    // ================= AI METHODS =================

    private async Task<string> AskAiWithHistoryAsync()
    {
        var messages = new List<object>
        {
            new
            {
                role = "system",
                content =
                "You are a helpful assistant. " +
                "Keep the conversation flowing naturally."
            }
        };

        foreach (var (role, content) in _chatHistory.TakeLast(6))
            messages.Add(new { role, content });

        return await SendAsync(messages);
    }

    private async Task<string> AskWithContextAndHistoryAsync(
        string context)
    {
        var messages = new List<object>
        {
            new
            {
                role = "system",
                content =
                "Answer ONLY using the provided context. " +
                "If the answer is not found, say you don't know."
            },
            new
            {
                role = "system",
                content = $"Context:\n{context}"
            }
        };

        foreach (var (role, content) in _chatHistory.TakeLast(6))
            messages.Add(new { role, content });

        return await SendAsync(messages);
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
            "Improve the answer for clarity, completeness, and professionalism. " +
            "You MAY add short, useful recommendations or mention related roles " +
            "ONLY if they are logically implied by the context. " +
            "Do NOT invent names, facts, or internal company information."
        },
        new
        {
            role = "user",
            content =
            $"Question:\n{question}\n\n" +
            $"Answer:\n{baseAnswer}"
        }
    };

        return await SendAsync(messages, maxTokens: 180, temperature: 0.45);
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
                "Bearer", _config["OpenAI:ApiKey"]);

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
}

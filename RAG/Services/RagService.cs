//using RAG.Models;
//using System.Net.Http.Json;
//using System.Text.Json.Serialization;

namespace RAG.Services;

//public class RagService
//{
//    private readonly HttpClient _httpClient;
//    private readonly string _apiKey;
//    private readonly string _model;
//    private readonly string _baseUrl;
//    private readonly int _maxTokens;
//    private readonly double _temperature;
//    private readonly string _blogPostsDirectory;
//    private readonly ILogger<RagService> _logger;
//    private readonly List<ChatMessage> _conversationHistory = new();

//    public RagService(
//        IHttpClientFactory httpClientFactory,
//        IConfiguration configuration,
//        ILogger<RagService> logger)
//    {
//        _httpClient = httpClientFactory.CreateClient();
//        _apiKey = configuration["OpenAI:ApiKey"]
//            ?? throw new ArgumentNullException("OpenAI:ApiKey");
//        if (string.IsNullOrWhiteSpace(_apiKey))
//            throw new ArgumentException("OpenAI API Key is empty!");

//        _model = configuration["Model"] ?? "gpt-4o-mini";
//        _baseUrl = configuration["BaseUrl"] ?? "https://api.openai.com/v1/";
//        _maxTokens = int.Parse(configuration["MaxTokens"] ?? "2000");
//        _temperature = double.Parse(configuration["Temperature"] ?? "0.7");
//        _blogPostsDirectory = configuration["BlogPostsDirectory"]
//            ?? throw new ArgumentNullException("BlogPostsDirectory");
//        _logger = logger;
//    }

//    public async Task<string> AskAsync(string userMessage)
//    {
//        var relevantDocuments = await FindRelevantDocumentsAsync(userMessage);

//        var context = string.Join(
//            "\n\n",
//            relevantDocuments.Select(x => $"Title: {x.Title}\n{x.Content}")
//        );

//        _conversationHistory.Add(new ChatMessage
//        {
//            Role = "user",
//            Content = userMessage
//        });

//        var messages = new List<ChatMessage>
//        {
//            new ChatMessage
//            {
//                Role = "system",
//                Content = "Answer using the context. If answer not in context, say 'I don't know'."
//            },
//            new ChatMessage
//            {
//                Role = "system",
//                Content = $"Context:\n{context}"
//            }
//        };
//        messages.AddRange(_conversationHistory);

//        var requestBody = new
//        {
//            model = _model,
//            messages = messages,
//            max_tokens = _maxTokens,
//            temperature = _temperature
//        };

//        var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl.TrimEnd('/')}/chat/completions");
//        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);
//        request.Content = JsonContent.Create(requestBody);

//        var response = await _httpClient.SendAsync(request);

//        if (!response.IsSuccessStatusCode)
//        {
//            var error = await response.Content.ReadAsStringAsync();
//            _logger.LogError($"OpenAI API Error: {error}");
//            throw new Exception($"OpenAI API Error: {error}");
//        }

//        var result = await response.Content.ReadFromJsonAsync<OpenAIResponse>();
//        var answer = result?.Choices?[0]?.Message?.Content ?? "لم أستطع الحصول على إجابة";

//        _conversationHistory.Add(new ChatMessage
//        {
//            Role = "assistant",
//            Content = answer
//        });

//        return answer;
//    }

//    private async Task<List<Item>> FindRelevantDocumentsAsync(string userMessage)
//    {
//        var relevantDocuments = new List<Item>();
//        var directory = new DirectoryInfo(_blogPostsDirectory);
//        if (!directory.Exists) return relevantDocuments;

//        var userEmbedding = await GetEmbeddingAsync(userMessage);

//        foreach (var blogPostDir in directory.GetDirectories("*", SearchOption.AllDirectories))
//        {
//            var indexFile = Path.Combine(blogPostDir.FullName, "index.md");
//            if (!File.Exists(indexFile)) continue;

//            var content = await File.ReadAllTextAsync(indexFile);
//            foreach (var chunk in ChunkText(content))
//            {
//                var chunkEmbedding = await GetEmbeddingAsync(chunk);
//                var similarity = CosineSimilarity(userEmbedding, chunkEmbedding);
//                if (similarity > 0.55f)
//                {
//                    relevantDocuments.Add(new Item
//                    {
//                        Title = blogPostDir.Name,
//                        Content = chunk,
//                        Score = (int)(similarity * 100),
//                        Embedding = chunkEmbedding
//                    });
//                    _logger.LogInformation($"Similarity: {similarity}");

//                }
//            }
//        }

//        return relevantDocuments.OrderByDescending(d => d.Score).Take(5).ToList();
//    }

//    private List<string> ChunkText(string text, int maxWords = 200)
//    {
//        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
//        var chunks = new List<string>();
//        for (int i = 0; i < words.Length; i += maxWords)
//            chunks.Add(string.Join(" ", words.Skip(i).Take(maxWords)));
//        return chunks;
//    }

//    private async Task<float[]> GetEmbeddingAsync(string text)
//    {
//        if (string.IsNullOrWhiteSpace(text))
//            return new float[1536]; // نص فارغ → embedding فارغ

//        var requestBody = new
//        {
//            model = "text-embedding-3-small",
//            input = text
//        };

//        try
//        {
//            var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl.TrimEnd('/')}/embeddings");
//            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);
//            request.Content = JsonContent.Create(requestBody);

//            var response = await _httpClient.SendAsync(request);

//            if (!response.IsSuccessStatusCode)
//            {
//                var error = await response.Content.ReadAsStringAsync();
//                _logger.LogWarning($"Embedding failed: {error}");
//                return new float[1536];
//            }

//            var result = await response.Content.ReadFromJsonAsync<EmbeddingResponse>();
//            return result?.Data?[0]?.Embedding ?? new float[1536];
//        }
//        catch (Exception ex)
//        {
//            _logger.LogWarning($"Embedding request exception: {ex.Message}");
//            return new float[1536];
//        }
//    }
//    //private async Task<float[]> GetEmbeddingAsync(string text)
//    //{
//    //    if (string.IsNullOrWhiteSpace(text))
//    //        return Array.Empty<float>();

//    //    var requestBody = new
//    //    {
//    //        model = "nomic-embed-text",
//    //        prompt = text
//    //    };

//    //    try
//    //    {
//    //        var response = await _httpClient.PostAsJsonAsync(
//    //            "http://localhost:11434/api/embeddings",
//    //            requestBody
//    //        );

//    //        if (!response.IsSuccessStatusCode)
//    //        {
//    //            _logger.LogWarning("Ollama embedding failed");
//    //            return Array.Empty<float>();
//    //        }

//    //        var result = await response.Content.ReadFromJsonAsync<OllamaEmbeddingResponse>();
//    //        return result?.Embedding ?? Array.Empty<float>();
//    //    }
//    //    catch (Exception ex)
//    //    {
//    //        _logger.LogError($"Ollama embedding error: {ex.Message}");
//    //        return Array.Empty<float>();
//    //    }
//    //}


//    private static float CosineSimilarity(float[] vectorA, float[] vectorB)
//    {
//        if (vectorA.Length == 0 || vectorB.Length == 0) return 0f;

//        float dot = 0f, magA = 0f, magB = 0f;
//        for (int i = 0; i < vectorA.Length; i++)
//        {
//            dot += vectorA[i] * vectorB[i];
//            magA += vectorA[i] * vectorA[i];
//            magB += vectorB[i] * vectorB[i];
//        }
//        return dot / ((float)Math.Sqrt(magA) * (float)Math.Sqrt(magB));
//    }

//    // Models
//    private class ChatMessage
//    {
//        [JsonPropertyName("role")]
//        public string? Role { get; set; }

//        [JsonPropertyName("content")]
//        public string? Content { get; set; }
//    }

//    private class OpenAIResponse
//    {
//        [JsonPropertyName("choices")]
//        public Choice[]? Choices { get; set; }
//    }

//    private class Choice
//    {
//        [JsonPropertyName("message")]
//        public ChatMessage? Message { get; set; }
//    }

//    private class EmbeddingResponse
//    {
//        [JsonPropertyName("data")]
//        public EmbeddingData[]? Data { get; set; }
//    }

//    private class EmbeddingData
//    {
//        [JsonPropertyName("embedding")]
//        public float[]? Embedding { get; set; }
//    }
//    private class OllamaEmbeddingResponse
//    {
//        [JsonPropertyName("embedding")]
//        public float[]? Embedding { get; set; }
//    }

//}
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
        var embeddings = await _embedding.CreateBatchEmbeddingAsync(
            new List<string> { question });

        var qEmbedding = embeddings.First();
        // 2️⃣ Search in vector store
        var results = _store.Search(qEmbedding, 5).ToList();

        // 3️⃣ لو مفيش نتائج خالص → AI
        if (!results.Any())
        {
            return new ChatResponse
            {
                Source = "ai",
                Answer = await AskAiAsync(question)
            };
        }

        // 4️⃣ لو في نتائج لكن ضعيفة جدًا
        var bestScore = results.First().Score;

        if (bestScore < 0.55f)
        {
            return new ChatResponse
            {
                Source = "ai",
                Answer = await AskAiAsync(question)
            };
        }
        var context = string.Join("\n\n",
            results.Select(r => r.Chunk.Content));

        return new ChatResponse
        {
            Source = "data",
            Answer = await AskWithContextAsync(question, context)
        };
    }



    private async Task<string> AskWithContextAsync(string question, string context)
    {
        var messages = new[]
        {
            new { role = "system", content = "Answer ONLY from context." },
            new { role = "system", content = $"Context:\n{context}" },
            new { role = "user", content = question }
        };

        return await SendAsync(messages);
    }

    private async Task<string> AskAiAsync(string question)
    {
        var messages = new[]
        {
            new { role = "system", content = "You are a helpful assistant." },
            new { role = "user", content = question }
        };

        return await SendAsync(messages);
    }

    private async Task<string> SendAsync(object messages)
    {
        var body = new
        {
            model = _config["Model"],
            messages = messages,
            max_tokens = int.Parse(_config["MaxTokens"]!),
            temperature = double.Parse(_config["Temperature"]!)
        };

        using var req = new HttpRequestMessage(
            HttpMethod.Post,
            "https://api.openai.com/v1/chat/completions");

        req.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", _config["OpenAI:ApiKey"]);

        req.Content = JsonContent.Create(body);

        var res = await _http.SendAsync(req);
        res.EnsureSuccessStatusCode();

        using var json = await JsonDocument.ParseAsync(
            await res.Content.ReadAsStreamAsync());

        return json
            .RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString()!;
    }
    #region improveWithAI
    //public async Task<ChatResponse> AskAsync(string question)
    //{
    //    var embeddings = await _embedding.CreateBatchEmbeddingAsync(
    //        new List<string> { question });

    //    var qEmbedding = embeddings.First();

    //    var results = _store.Search(qEmbedding, 3);

    //    var bestScore = results
    //        .Select(r => r.Score)
    //        .DefaultIfEmpty(0f)
    //        .Max();

    //    if (bestScore < 0.65f)
    //    {
    //        return new ChatResponse
    //        {
    //            Source = "ai",
    //            Answer = await AskAiAsync(question)
    //        };
    //    }

    //    var context = string.Join("\n\n",
    //        results.Select(r => r.Chunk.Content));

    //    var baseAnswer = await AskWithContextAsync(question, context);
    //    var improved = await ImproveWithAI(baseAnswer, question);

    //    return new ChatResponse
    //    {
    //        Source = "knowledge + ai",
    //        Answer = improved
    //    };
    //}
    //private async Task<string> ImproveWithAI(string baseAnswer, string question)
    //{
    //    var messages = new[]
    //    {
    //    new
    //    {
    //        role = "system",
    //        content = "You are a senior software architect. Improve the answer and add smart, practical suggestions without changing the factual meaning."
    //    },
    //    new
    //    {
    //        role = "user",
    //        content = $"Question: {question}\nCurrent answer: {baseAnswer}"
    //    }
    //};

    //    return await SendAsync(messages);
    //}

    #endregion
}

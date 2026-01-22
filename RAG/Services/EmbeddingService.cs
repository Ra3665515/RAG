using RAGChat.Models;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;

namespace RAGChat.Services;

public class EmbeddingService
{
    private readonly HttpClient _http;
    private readonly string _apiKey;
    private readonly string _path =
       Path.Combine("Data", "embeddings.json");

    private static readonly object _lock = new();


    public EmbeddingService(HttpClient http, IConfiguration config)
    {
        _http = http;
        _apiKey = config["OpenAI:ApiKey"]!;
    }
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
                    new JsonSerializerOptions { WriteIndented = true }
                )
            );
        }
    }

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
                    new JsonSerializerOptions { WriteIndented = true }
                )
            );
        }
    }

    ////openai
    //public async Task<List<float[]>> CreateBatchEmbeddingAsync(List<string> texts)
    //{
    //    var results = new List<float[]>();
    //    const int batchSize = 20;

    //    for (int i = 0; i < texts.Count; i += batchSize)
    //    {
    //        var batch = texts.Skip(i).Take(batchSize).ToList();

    //        var body = new
    //        {
    //            model = "text-embedding-3-small",
    //            input = batch
    //        };

    //        HttpResponseMessage res = null!;

    //        for (int attempt = 0; attempt < 3; attempt++)
    //        {
    //            using var req = new HttpRequestMessage(
    //                HttpMethod.Post,
    //                "https://api.openai.com/v1/embeddings");

    //            req.Headers.Authorization =
    //                new AuthenticationHeaderValue("Bearer", _apiKey);

    //            req.Content = JsonContent.Create(body);

    //            res = await _http.SendAsync(req);

    //            if (res.IsSuccessStatusCode)
    //                break;

    //            if (res.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
    //                await Task.Delay(2000);
    //            else
    //                res.EnsureSuccessStatusCode();
    //        }

    //        res.EnsureSuccessStatusCode();

    //        // ✅ هنا التعديل
    //        var json = await res.Content.ReadFromJsonAsync<JsonDocument>();

    //        var data = json!.RootElement.GetProperty("data");

    //        foreach (var item in data.EnumerateArray())
    //        {
    //            var embedding = item
    //                .GetProperty("embedding")
    //                .EnumerateArray()
    //                .Select(x => x.GetSingle())
    //                .ToArray();

    //            results.Add(embedding);
    //        }

    //        await Task.Delay(1000);
    //    }

    //    return results;
    //}
    public async Task<List<float[]>> CreateBatchEmbeddingAsync(List<string> texts)
    {
        var results = new List<float[]>();

        foreach (var text in texts)
        {
            var body = new
            {
                model = "nomic-embed-text",
                prompt = text
            };

            var res = await _http.PostAsJsonAsync(
                "http://localhost:11434/api/embeddings",
                body);

            res.EnsureSuccessStatusCode();

            var json = await res.Content.ReadFromJsonAsync<JsonDocument>();

            var embedding = json!.RootElement
                .GetProperty("embedding")
                .EnumerateArray()
                .Select(x => x.GetSingle())
                .ToArray();

            results.Add(embedding);
        }

        return results;
    }


}

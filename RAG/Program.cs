using RAG.Services;
using RAGChat.Models;
using RAGChat.Services;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// ===================== Services =====================
builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHttpClient();
builder.Services.AddSingleton<LocalVectorStore>();
builder.Services.AddSingleton<EmbeddingService>();
builder.Services.AddSingleton<RagService>();

var app = builder.Build();

// ===================== Middleware =====================
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();

// ===================== Load Knowledge =====================
var store = app.Services.GetRequiredService<LocalVectorStore>();
var embedding = app.Services.GetRequiredService<EmbeddingService>();

var embeddingsPath = Path.Combine("Data", "embeddings.json");
var knowledgePath = Path.Combine("Data", "knowledge.txt");

// تأكدي إن فولدر Data موجود
Directory.CreateDirectory("Data");

if (File.Exists(embeddingsPath) &&
    new FileInfo(embeddingsPath).Length > 10)
{
    var json = File.ReadAllText(embeddingsPath);

    var cached = JsonSerializer.Deserialize<List<DocumentChunk>>(json);

    if (cached != null)
    {
        foreach (var doc in cached)
            store.Add(doc);
    }
}
else
{
    if (!File.Exists(knowledgePath))
        throw new FileNotFoundException(
            "knowledge.txt not found", knowledgePath);

    var lines = File.ReadAllLines(knowledgePath)
        .Where(l => !string.IsNullOrWhiteSpace(l))
        .ToList();

    var embeddings = await embedding.CreateBatchEmbeddingAsync(lines);

    var docs = new List<DocumentChunk>();

    for (int i = 0; i < lines.Count; i++)
    {
        docs.Add(new DocumentChunk
        {
            Content = lines[i],
            Embedding = embeddings[i]
        });
    }

    File.WriteAllText(
        embeddingsPath,
        JsonSerializer.Serialize(
            docs,
            new JsonSerializerOptions { WriteIndented = true }
        )
    );

    foreach (var d in docs)
        store.Add(d);
}

// ===================== Endpoints =====================
app.MapControllers();



app.Run();

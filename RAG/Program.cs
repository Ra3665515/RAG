using RAG.Services;
using RAGChat.Models;
using RAGChat.Services;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Services 
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHttpClient();

// Local ANN Store
builder.Services.AddSingleton<LocalVectorStore>();
builder.Services.AddSingleton<AnswerCache>();


// AI Services
builder.Services.AddSingleton<EmbeddingPersistenceService>();

builder.Services.AddSingleton<EmbeddingService>();
builder.Services.AddSingleton<RagService>();

var app = builder.Build();

//  Middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();

// Load Knowledge 
var store = app.Services.GetRequiredService<LocalVectorStore>();
var embedding = app.Services.GetRequiredService<EmbeddingService>();

var embeddingsPath = Path.Combine("Data", "embeddings.json");
var knowledgePath = Path.Combine("Data", "knowledge.txt");

Directory.CreateDirectory("Data");

//embeddings cached
if (File.Exists(embeddingsPath) &&
    new FileInfo(embeddingsPath).Length > 10)
{
    var json = File.ReadAllText(embeddingsPath);

    var cached =
        JsonSerializer.Deserialize<List<DocumentChunk>>(json);

    if (cached != null && cached.Any())
    {
        store.Add(cached);
    }
}
//   generate embeddings 
else
{
    if (!File.Exists(knowledgePath))
        throw new FileNotFoundException(
            "knowledge.txt not found", knowledgePath);

    var lines = File.ReadAllLines(knowledgePath)
        .Where(l => !string.IsNullOrWhiteSpace(l))
        .ToList();

    // Create embeddings
    var embeddings =
        await embedding.CreateBatchEmbeddingAsync(lines);

    var docs = new List<DocumentChunk>();

    for (int i = 0; i < lines.Count; i++)
    {
        //docs.Add(new DocumentChunk
        //{
        //    Content = lines[i],
        //    Embedding = embeddings[i]
        //});
        docs.Add(new DocumentChunk
        {
            Content = lines[i],
            Embedding = embeddings[i],
            Metadata = new ChunkMetadata
            {
                Category = CategoryDetector.DetectRuleBased(lines[i]),
                Source = "knowledge"
            }
        });

    }

    // Cache embeddings
    File.WriteAllText(
        embeddingsPath,
        JsonSerializer.Serialize(
            docs,
            new JsonSerializerOptions { WriteIndented = true }
        )
    );

    store.Add(docs);
}
app.MapGet("/", () => Results.Redirect("/swagger"));




//  Endpoints 
app.MapControllers();

app.Run();

using Microsoft.AspNetCore.Mvc;
using RAG.Services;
using RAGChat.Services;

namespace RAGChat.Controllers;

[ApiController]
[Route("api/chat")]
public class ChatController : ControllerBase
{
    private readonly RagService _rag;

    public ChatController(RagService rag)
    {
        _rag = rag;
    }

    [HttpPost("ask")]
    public async Task<IActionResult> Ask([FromBody] AskRequest req)
    {
        var answer = await _rag.AskAsync(req.Question);
        return Ok(new { answer });
    }
    [HttpGet("test-embedding")]
    public async Task<IActionResult> TestEmbedding(
    [FromServices] EmbeddingService embedding)
    {
        var vector = await embedding.CreateBatchEmbeddingAsync(
            new() { "What is HR?" });

        return Ok(new { length = vector[0].Length });
    }
    [HttpGet("test-search")]
    public IActionResult TestSearch(
        [FromServices] LocalVectorStore store)
    {
        var results = store.Search(
            new float[768], // dummy
            5);

        return Ok(results.Count());
    }

}


public class AskRequest
{
    public string Question { get; set; } = "";
}

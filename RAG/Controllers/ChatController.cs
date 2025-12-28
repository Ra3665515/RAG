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
}

public class AskRequest
{
    public string Question { get; set; } = "";
}

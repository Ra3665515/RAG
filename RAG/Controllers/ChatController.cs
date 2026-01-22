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
    [HttpPost("feedback")]
    public async Task<IActionResult> Feedback(
       [FromBody] FeedbackRequest request)
    {
        var accepted =
            await _rag.LearnFromLastInteractionAsync(
                request.CorrectAnswer);

        if (!accepted)
            return BadRequest(new
            {
                status = "rejected",
                reason = "Correction not validated by AI"
            });

        return Ok(new { status = "learned" });
    }


}

public class FeedbackRequest
{
    public string CorrectAnswer { get; set; } = "";
}
public class ChatInteraction
{
    public string Question { get; set; } = "";
    public string Answer { get; set; } = "";
    public string Category { get; set; } = "general";
}

public class AskRequest
{
    public string Question { get; set; } = "";
}

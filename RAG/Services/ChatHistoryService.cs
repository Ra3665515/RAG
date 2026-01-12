using MongoDB.Driver;
using RAG.Models;

namespace RAG.Services;
public class ChatHistoryService
{
    private readonly IMongoCollection<ChatConversation> _collection;

    public ChatHistoryService(IMongoDatabase db)
    {
        _collection = db.GetCollection<ChatConversation>("chat_conversations");
    }

    public async Task<ChatConversation> GetAsync(string conversationId)
    {
        return await _collection.Find(x => x.Id == conversationId)
            .FirstOrDefaultAsync()
            ?? new ChatConversation
            {
                Id = conversationId,
                CreatedAt = DateTime.UtcNow
            };
    }

    public async Task SaveAsync(ChatConversation conversation)
    {
        await _collection.ReplaceOneAsync(
            x => x.Id == conversation.Id,
            conversation,
            new ReplaceOptions { IsUpsert = true });
    }
}


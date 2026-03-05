using Capstone.Data;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using static Capstone.Models.NomsaurModel;

namespace Capstone
{
    public class DirectMessageHub : Hub
    {
        private readonly AppDbContext _context;
        private readonly ILogger<DirectMessageHub> _logger;

        public DirectMessageHub(AppDbContext context, ILogger<DirectMessageHub> logger)
        {
            _context = context;
            _logger = logger;
        }

        private int? GetCurrentUserId()
        {
            return Context.GetHttpContext()?.Session?.GetInt32("UserId");
        }

        private static string ConversationGroup(int conversationId) => $"dm:conv:{conversationId}";

        public async Task JoinConversation(int conversationId)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                throw new HubException("Unauthorized");

            var convo = await _context.DirectConversations
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.ConversationId == conversationId);

            if (convo == null)
                throw new HubException("Conversation not found");

            if (convo.CustomerUserId != userId.Value && convo.OwnerUserId != userId.Value)
                throw new HubException("Forbidden");

            await Groups.AddToGroupAsync(Context.ConnectionId, ConversationGroup(conversationId));
        }

        public async Task LeaveConversation(int conversationId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, ConversationGroup(conversationId));
        }

        public async Task SendMessage(int conversationId, string body)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                throw new HubException("Unauthorized");

            body = (body ?? string.Empty).Trim();
            if (body.Length == 0)
                throw new HubException("Message cannot be empty");
            if (body.Length > 2000)
                throw new HubException("Message too long");

            var convo = await _context.DirectConversations
                .Include(c => c.CustomerUser)
                .Include(c => c.OwnerUser)
                .FirstOrDefaultAsync(c => c.ConversationId == conversationId);

            if (convo == null)
                throw new HubException("Conversation not found");

            if (convo.CustomerUserId != userId.Value && convo.OwnerUserId != userId.Value)
                throw new HubException("Forbidden");

            var msg = new DirectMessage
            {
                ConversationId = conversationId,
                SenderUserId = userId.Value,
                Body = body,
                SentAt = DateTime.UtcNow
            };

            _context.DirectMessages.Add(msg);
            convo.LastMessageAt = msg.SentAt;
            await _context.SaveChangesAsync();

            var payload = new
            {
                messageId = msg.MessageId,
                conversationId,
                senderUserId = msg.SenderUserId,
                senderName = (msg.SenderUserId == convo.CustomerUserId ? convo.CustomerUser.Name : convo.OwnerUser.Name),
                body = msg.Body,
                sentAt = msg.SentAt
            };

            await Clients.Group(ConversationGroup(conversationId)).SendAsync("message", payload);
        }
    }
}


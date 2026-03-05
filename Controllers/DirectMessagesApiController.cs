using Capstone.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using static Capstone.Models.NomsaurModel;

namespace Capstone.Controllers
{
    [ApiController]
    [Route("api/directmessages")]
    public class DirectMessagesApiController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IHubContext<Capstone.DirectMessageHub> _hub;
        private readonly ILogger<DirectMessagesApiController> _logger;

        public DirectMessagesApiController(AppDbContext context, IHubContext<Capstone.DirectMessageHub> hub, ILogger<DirectMessagesApiController> logger)
        {
            _context = context;
            _hub = hub;
            _logger = logger;
        }

        private int? CurrentUserId => HttpContext.Session.GetInt32("UserId");

        private static string ConversationGroup(int conversationId) => $"dm:conv:{conversationId}";

        public class CreateConversationRequest
        {
            public string PlaceId { get; set; } = string.Empty;
        }

        public class SendMessageRequest
        {
            public string Body { get; set; } = string.Empty;
        }

        // Create or get a conversation with the verified owner of the restaurant.
        [HttpPost("conversations")]
        public async Task<IActionResult> CreateConversation([FromBody] CreateConversationRequest request)
        {
            var userId = CurrentUserId;
            if (!userId.HasValue)
                return Unauthorized(new { message = "User not authenticated" });

            var placeId = (request.PlaceId ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(placeId))
                return BadRequest(new { message = "placeId is required" });

            var owner = await _context.RestaurantOwners
                .AsNoTracking()
                .FirstOrDefaultAsync(ro => ro.PlaceId == placeId && ro.VerificationStatus == "Verified");

            if (owner == null)
                return NotFound(new { message = "This restaurant does not have a verified owner yet." });

            if (owner.UserId == userId.Value)
                return BadRequest(new { message = "Owner cannot start a conversation with themselves." });

            var existing = await _context.DirectConversations
                .FirstOrDefaultAsync(c =>
                    c.PlaceId == placeId &&
                    c.CustomerUserId == userId.Value &&
                    c.OwnerUserId == owner.UserId);

            if (existing != null)
            {
                return Ok(new
                {
                    conversationId = existing.ConversationId,
                    placeId = existing.PlaceId,
                    customerUserId = existing.CustomerUserId,
                    ownerUserId = existing.OwnerUserId,
                    createdAt = existing.CreatedAt,
                    lastMessageAt = existing.LastMessageAt
                });
            }

            var convo = new DirectConversation
            {
                PlaceId = placeId,
                CustomerUserId = userId.Value,
                OwnerUserId = owner.UserId,
                CreatedAt = DateTime.UtcNow
            };

            _context.DirectConversations.Add(convo);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                conversationId = convo.ConversationId,
                placeId = convo.PlaceId,
                customerUserId = convo.CustomerUserId,
                ownerUserId = convo.OwnerUserId,
                createdAt = convo.CreatedAt,
                lastMessageAt = convo.LastMessageAt
            });
        }

        // List conversations for the current user (as customer or as owner).
        [HttpGet("conversations")]
        public async Task<IActionResult> ListConversations()
        {
            var userId = CurrentUserId;
            if (!userId.HasValue)
                return Unauthorized(new { message = "User not authenticated" });

            var uid = userId.Value;

            var items = await _context.DirectConversations
                .AsNoTracking()
                .Where(c => c.CustomerUserId == uid || c.OwnerUserId == uid)
                .OrderByDescending(c => c.LastMessageAt ?? c.CreatedAt)
                .Select(c => new
                {
                    c.ConversationId,
                    c.PlaceId,
                    c.CustomerUserId,
                    c.OwnerUserId,
                    c.CreatedAt,
                    c.LastMessageAt,
                    role = (c.OwnerUserId == uid ? "Owner" : "Customer"),
                    otherUserId = (c.OwnerUserId == uid ? c.CustomerUserId : c.OwnerUserId),
                    customerName = _context.Users.Where(u => u.UserId == c.CustomerUserId).Select(u => u.Name).FirstOrDefault(),
                    ownerName = _context.Users.Where(u => u.UserId == c.OwnerUserId).Select(u => u.Name).FirstOrDefault(),
                    otherUserName = (c.OwnerUserId == uid
                        ? _context.Users.Where(u => u.UserId == c.CustomerUserId).Select(u => u.Name).FirstOrDefault()
                        : _context.Users.Where(u => u.UserId == c.OwnerUserId).Select(u => u.Name).FirstOrDefault())
                    ,
                    lastMessage = _context.DirectMessages
                        .Where(m => m.ConversationId == c.ConversationId)
                        .OrderByDescending(m => m.SentAt)
                        .Select(m => new
                        {
                            m.Body,
                            m.SentAt,
                            m.SenderUserId,
                            senderName = _context.Users.Where(u => u.UserId == m.SenderUserId).Select(u => u.Name).FirstOrDefault()
                        })
                        .FirstOrDefault()
                })
                .ToListAsync();

            return Ok(new { items });
        }

        [HttpGet("conversations/{conversationId:int}/messages")]
        public async Task<IActionResult> GetMessages(int conversationId, [FromQuery] int limit = 50)
        {
            var userId = CurrentUserId;
            if (!userId.HasValue)
                return Unauthorized(new { message = "User not authenticated" });

            limit = Math.Clamp(limit, 1, 200);

            var convo = await _context.DirectConversations
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.ConversationId == conversationId);

            if (convo == null)
                return NotFound(new { message = "Conversation not found" });

            if (convo.CustomerUserId != userId.Value && convo.OwnerUserId != userId.Value)
                return Forbid();

            var messages = await _context.DirectMessages
                .AsNoTracking()
                .Where(m => m.ConversationId == conversationId)
                .OrderByDescending(m => m.SentAt)
                .Take(limit)
                .OrderBy(m => m.SentAt)
                .Select(m => new
                {
                    m.MessageId,
                    m.ConversationId,
                    m.SenderUserId,
                    senderName = _context.Users.Where(u => u.UserId == m.SenderUserId).Select(u => u.Name).FirstOrDefault(),
                    m.Body,
                    m.SentAt,
                    m.ReadAt
                })
                .ToListAsync();

            return Ok(new { conversationId, placeId = convo.PlaceId, customerUserId = convo.CustomerUserId, ownerUserId = convo.OwnerUserId, messages });
        }

        // REST fallback: send a message and broadcast to SignalR group.
        [HttpPost("conversations/{conversationId:int}/messages")]
        public async Task<IActionResult> SendMessage(int conversationId, [FromBody] SendMessageRequest request)
        {
            var userId = CurrentUserId;
            if (!userId.HasValue)
                return Unauthorized(new { message = "User not authenticated" });

            var body = (request.Body ?? string.Empty).Trim();
            if (body.Length == 0)
                return BadRequest(new { message = "Message cannot be empty" });
            if (body.Length > 2000)
                return BadRequest(new { message = "Message too long" });

            var convo = await _context.DirectConversations
                .Include(c => c.CustomerUser)
                .Include(c => c.OwnerUser)
                .FirstOrDefaultAsync(c => c.ConversationId == conversationId);

            if (convo == null)
                return NotFound(new { message = "Conversation not found" });

            if (convo.CustomerUserId != userId.Value && convo.OwnerUserId != userId.Value)
                return Forbid();

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

            try
            {
                await _hub.Clients.Group(ConversationGroup(conversationId)).SendAsync("message", payload);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to broadcast DM via SignalR");
            }

            return Ok(payload);
        }
    }
}


using AIIntegrationsAPI.Abstractions;
using AIIntegrationsAPI.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

[ApiController] 
[Route("api/[controller]")]
public sealed class ChatController : ControllerBase
{
    private readonly IChatProviderFactory _factory;
    private readonly IChatSessionStore _sessions;
    private readonly ILogger<ChatController> _logger;

    public ChatController(IChatProviderFactory factory, IChatSessionStore sessions, ILogger<ChatController> logger)
    {
        _factory = factory;
        _sessions = sessions;
        _logger = logger;
    }

    [HttpPost("complete")]
    public async Task<ActionResult<ChatResponse>> Complete(
        [FromBody] ChatRequest request,
        [FromQuery] string? sessionId,
        [FromQuery] string? provider,
        CancellationToken ct)
    {
        // Resolve session
        var sid = string.IsNullOrWhiteSpace(sessionId)
            ? Request.Cookies["sid"]
            : sessionId;

        if (string.IsNullOrWhiteSpace(sid))
        {
            sid = Guid.NewGuid().ToString("n");
            Response.Cookies.Append("sid", sid, new CookieOptions
            {
                HttpOnly = false, // Angular can read it if you want; use true if only server needs it
                Secure = true,
                SameSite = SameSiteMode.Lax,
                Expires = DateTimeOffset.UtcNow.AddDays(2)
            });
        }

        // Build the message list for this turn
        var history = (await _sessions.GetAsync(sid, ct)).ToList();

        // Back-compat: support one-shot Prompt or full Messages
        if (request.Messages is { Count: > 0 })
        {
            // You might validate roles here
            history.AddRange(request.Messages);
        }
        //else if (!string.IsNullOrWhiteSpace(request.Prompt))
        //{
        //    history.Add(new ChatMessage { Role = "user", Content = request.Prompt });
        //}
        else
        {
            return BadRequest("Provide either 'prompt' or 'messages'.");
        }

        // Optional: trim history to fit token budget (simple heuristic)
        history = TrimHistory(history, maxMessages: 30);

        var chosen = request.Provider ?? provider ?? string.Empty;
        if (string.Equals(chosen, "OpenAI", StringComparison.OrdinalIgnoreCase))
        {
            return Ok(new ChatResponse { Text = "OpenAI support not implemented yet.", SessionId = sid });
        }

        var client = _factory.Create(chosen);

        _logger.LogInformation("Chat request > Provider={Provider} Session={SessionId} Messages={Count}",
            string.IsNullOrEmpty(chosen) ? "(default)" : chosen, sid, history.Count);

        var assistant = await client.CompleteAsync(history, ct);

        // Persist the assistant turn
        await _sessions.AppendAsync(sid, new[]
        {
            new ChatMessage { Role = "assistant", Content = assistant }
        }, ct);

        return Ok(new ChatResponse { Text = assistant, SessionId = sid });
    }

    [HttpPost("new-session")]
    public ActionResult NewSession()
    {
        var sid = Guid.NewGuid().ToString("n");
        Response.Cookies.Append("sid", sid, new CookieOptions { Secure = true, SameSite = SameSiteMode.Lax });
        return Ok(new { sessionId = sid });
    }

    [HttpPost("reset")]
    public async Task<IActionResult> Reset([FromQuery] string sessionId, CancellationToken ct)
    {
        await _sessions.ResetAsync(sessionId, ct);
        return NoContent();
    }

    private static List<ChatMessage> TrimHistory(List<ChatMessage> msgs, int maxMessages)
    {
        if (msgs.Count <= maxMessages) return msgs;
        // keep the last N messages (simple, but works for a demo)
        return msgs.Skip(Math.Max(0, msgs.Count - maxMessages)).ToList();
    }
}


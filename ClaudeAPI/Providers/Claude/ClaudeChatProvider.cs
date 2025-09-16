using AIIntegrationsAPI.Abstractions;
using AIIntegrationsAPI.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace AIIntegrationsAPI.Providers.Claude
{
    /// <summary>Provider implementation for Anthropic Claude. Handles both single-response and streaming chat completions.</summary>
    public sealed class ClaudeChatProvider : IChatProvider
    {
        private readonly HttpClient _http;
        private readonly ProviderOptions _opt;
        private readonly ILogger<ClaudeChatProvider> _logger;

        /// <summary>Initializes a new instance of the ClaudeChatProvider class.</summary>
        /// <param name="http">The HTTP client used for API requests.</param>
        /// <param name="opt">Provider configuration options.</param>
        /// <param name="logger">Logger for diagnostics.</param>
        public ClaudeChatProvider(HttpClient http, ProviderOptions opt, ILogger<ClaudeChatProvider> logger)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
            _opt = opt ?? throw new ArgumentNullException(nameof(opt));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>Normalizes the role string to Claude-compatible values.</summary>
        private static string NormRole(string? role)
            => role?.ToLowerInvariant() switch
            {
                "assistant" => "assistant",
                "system" => "system",
                _ => "user"
            };

        /// <summary>Converts chat messages to the format expected by Claude API.</summary>
        private object[] ToClaudeMessages(IEnumerable<ChatMessage> messages)
        {
            // Simple text content; good for a demo.
            return messages.Select(m => new
            {
                role = NormRole(m.Role),
                content = m.Content ?? string.Empty
            }).ToArray<object>();
        }

        /// <summary>Sends a chat completion request to Claude and returns the generated response text.</summary>
        /// <param name="messages">Conversation history as a sequence of chat messages.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>The generated response text.</returns>
        public async Task<string> CompleteAsync(IEnumerable<ChatMessage> messages, CancellationToken ct = default)
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, new Uri(new Uri(_opt.BaseUrl), "/v1/messages"));

            req.Headers.Add("x-api-key", _opt.ApiKey);
            if (!string.IsNullOrWhiteSpace(_opt.ApiVersion))
                req.Headers.Add("anthropic-version", _opt.ApiVersion);
            req.Headers.UserAgent.ParseAdd("AIIntegrationsAPI/1.0 (+.NET 9)");

            var body = new
            {
                model = _opt.Model,
                max_tokens = _opt.MaxTokens,
                messages = ToClaudeMessages(messages)
            };

            req.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

            using var resp = await _http.SendAsync(req, ct).ConfigureAwait(false);
            var test = resp;
            resp.EnsureSuccessStatusCode();

            await using var s = await resp.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
            using var doc = await JsonDocument.ParseAsync(s, cancellationToken: ct).ConfigureAwait(false);

            // Anthropic: response.content[0].text
            return doc.RootElement.GetProperty("content")[0]
                     .GetProperty("text").GetString() ?? string.Empty;
        }

        /// <summary>Sends a streaming chat completion request to Claude and yields response tokens as they arrive.</summary>
        /// <param name="messages">Conversation history as a sequence of chat messages.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>An asynchronous stream of response text tokens.</returns>
        public async IAsyncEnumerable<string> StreamAsync(IEnumerable<ChatMessage> messages, [EnumeratorCancellation] CancellationToken ct = default)
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, new Uri(new Uri(_opt.BaseUrl), "/v1/messages"));

            req.Headers.Add("x-api-key", _opt.ApiKey);
            if (!string.IsNullOrWhiteSpace(_opt.ApiVersion))
                req.Headers.Add("anthropic-version", _opt.ApiVersion);
            req.Headers.UserAgent.ParseAdd("AIIntegrationsAPI/1.0 (+.NET 9)");

            var body = new
            {
                model = _opt.Model,
                max_tokens = _opt.MaxTokens,
                stream = true,
                messages = ToClaudeMessages(messages)
            };

            req.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

            using var resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
            resp.EnsureSuccessStatusCode();

            await using var s = await resp.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
            using var reader = new StreamReader(s);

            while (!reader.EndOfStream && !ct.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync().ConfigureAwait(false);
                if (line is null) break;
                if (!line.StartsWith("data: ", StringComparison.Ordinal)) continue;

                var payload = line["data: ".Length..];
                if (string.IsNullOrWhiteSpace(payload) || payload == "[DONE]") continue;

                using var evt = JsonDocument.Parse(payload);

                // { "delta": { "text": "..." }, ... }
                if (evt.RootElement.TryGetProperty("delta", out var delta) &&
                    delta.TryGetProperty("text", out var piece))
                {
                    var token = piece.GetString();
                    if (token is not null)
                        yield return token;
                }
            }
        }
    }
}


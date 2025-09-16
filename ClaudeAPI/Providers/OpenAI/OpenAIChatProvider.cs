using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AIIntegrationsAPI.Abstractions;
using AIIntegrationsAPI.Models;
using AIIntegrationsAPI.Options;
using Microsoft.Extensions.Logging;

namespace AIIntegrationsAPI.Providers.OpenAI
{
    /// <summary>OpenAI Chat Completions provider using /v1/chat/completions (non-stream + SSE stream). Implements IChatProvider for OpenAI's chat API.</summary>
    public sealed class OpenAIChatProvider : IChatProvider
    {
        private readonly HttpClient _http;
        private readonly ProviderOptions _opt;
        private readonly ILogger<OpenAIChatProvider> _logger;

        /// <summary>Initializes a new instance of the OpenAIChatProvider class.</summary>
        /// <param name="http">The HTTP client used for API requests.</param>
        /// <param name="opt">Provider configuration options.</param>
        /// <param name="logger">Logger for diagnostics.</param>
        public OpenAIChatProvider(HttpClient http, ProviderOptions opt, ILogger<OpenAIChatProvider> logger)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
            _opt = opt ?? throw new ArgumentNullException(nameof(opt));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>Normalizes the role string to OpenAI-compatible values.</summary>
        private static string NormRole(string? role)
            => role?.ToLowerInvariant() switch
            {
                "assistant" => "assistant",
                "system" => "system",
                _ => "user"
            };

        /// <summary>Converts chat messages to the format expected by OpenAI API.</summary>
        private object[] ToOpenAIMessages(IEnumerable<ChatMessage> messages)
        {
            return messages.Select(m => new
            {
                role = NormRole(m.Role),
                content = m.Content ?? string.Empty
            }).ToArray<object>();
        }

        /// <summary>Sends a chat completion request to OpenAI and returns the generated response text.</summary>
        /// <param name="messages">Conversation history as a sequence of chat messages.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>The generated response text.</returns>
        public async Task<string> CompleteAsync(IEnumerable<ChatMessage> messages, CancellationToken ct = default)
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, new Uri(new Uri(_opt.BaseUrl), "/v1/chat/completions"));

            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _opt.ApiKey);
            req.Headers.UserAgent.ParseAdd("AIIntegrationsAPI/1.0 (+.NET 9)");

            var body = new
            {
                model = _opt.Model,
                max_tokens = _opt.MaxTokens,
                messages = ToOpenAIMessages(messages)
            };

            req.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

            using var resp = await _http.SendAsync(req, ct).ConfigureAwait(false);
            resp.EnsureSuccessStatusCode();

            await using var s = await resp.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
            using var doc = await JsonDocument.ParseAsync(s, cancellationToken: ct).ConfigureAwait(false);

            // choices[0].message.content
            return doc.RootElement.GetProperty("choices")[0]
                     .GetProperty("message").GetProperty("content").GetString() ?? string.Empty;
        }

        /// <summary>Sends a streaming chat completion request to OpenAI and yields response tokens as they arrive.</summary>
        /// <param name="messages">Conversation history as a sequence of chat messages.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>An asynchronous stream of response text tokens.</returns>
        public async IAsyncEnumerable<string> StreamAsync(IEnumerable<ChatMessage> messages, [EnumeratorCancellation] CancellationToken ct = default)
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, new Uri(new Uri(_opt.BaseUrl), "/v1/chat/completions"));

            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _opt.ApiKey);
            req.Headers.UserAgent.ParseAdd("AIIntegrationsAPI/1.0 (+.NET 9)");

            var body = new
            {
                model = _opt.Model,
                max_tokens = _opt.MaxTokens,
                stream = true,
                messages = ToOpenAIMessages(messages)
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

                // OpenAI stream: choices[0].delta.content
                if (evt.RootElement.TryGetProperty("choices", out var choices) &&
                    choices.GetArrayLength() > 0 &&
                    choices[0].TryGetProperty("delta", out var delta) &&
                    delta.TryGetProperty("content", out var piece))
                {
                    var token = piece.GetString();
                    if (token is not null)
                        yield return token;
                }
            }
        }
    }
}


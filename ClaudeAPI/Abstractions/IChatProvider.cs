using AIIntegrationsAPI.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AIIntegrationsAPI.Abstractions
{
    /// <summary>
    /// Provider-agnostic chat interface. Implement once per vendor (Claude, OpenAI, etc.).
    /// </summary>
    public interface IChatProvider
    {
        /// <summary>Generates a complete response for the given chat messages.</summary>
        /// <param name="messages">The sequence of chat messages representing the conversation history. Each message should specify the sender's role and content.</param>
        /// <param name="ct">A cancellation token to cancel the operation if needed.</param>
        /// <returns>A task that resolves to the generated response text.</returns>
        Task<string> CompleteAsync(IEnumerable<ChatMessage> messages, CancellationToken ct);

        /// <summary>Streams generated response tokens for the given chat messages.</summary>
        /// <param name="messages">The sequence of chat messages representing the conversation history. Each message should specify the sender's role and content.</param>
        /// <param name="ct">A cancellation token to cancel the operation if needed.</param>
        /// <returns>An asynchronous stream of response text tokens.</returns>
        IAsyncEnumerable<string> StreamAsync(IEnumerable<ChatMessage> messages, CancellationToken ct);
    }
}

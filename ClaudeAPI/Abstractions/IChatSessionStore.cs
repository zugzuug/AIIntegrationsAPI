using AIIntegrationsAPI.Models; // Importing the ChatMessage model
using System.Collections.Generic; // For IEnumerable and IReadOnlyList
using System.Threading; // For CancellationToken
using System.Threading.Tasks; // For asynchronous programming

namespace AIIntegrationsAPI.Abstractions
{
    /// <summary>
    /// Interface for managing chat session storage.
    /// Provides methods to retrieve, append, and reset chat messages in a session.
    /// </summary>
    public interface IChatSessionStore
    {
        /// <summary>
        /// Retrieves all chat messages for a given session.
        /// </summary>
        /// <param name="sessionId">The unique identifier of the chat session.</param>
        /// <param name="ct">Cancellation token to cancel the operation if needed.</param>
        /// <returns>A read-only list of chat messages.</returns>
        Task<IReadOnlyList<ChatMessage>> GetAsync(string sessionId, CancellationToken ct);

        /// <summary>
        /// Appends new chat messages to an existing session.
        /// </summary>
        /// <param name="sessionId">The unique identifier of the chat session.</param>
        /// <param name="newMessages">The new chat messages to append.</param>
        /// <param name="ct">Cancellation token to cancel the operation if needed.</param>
        Task AppendAsync(string sessionId, IEnumerable<ChatMessage> newMessages, CancellationToken ct);

        /// <summary>
        /// Resets the chat session by clearing all messages.
        /// </summary>
        /// <param name="sessionId">The unique identifier of the chat session.</param>
        /// <param name="ct">Cancellation token to cancel the operation if needed.</param>
        Task ResetAsync(string sessionId, CancellationToken ct);
    }
}

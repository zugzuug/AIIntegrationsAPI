namespace AIIntegrationsAPI.Models
{
    /// <summary>Represents the response from the chat provider, containing the generated text and session ID.</summary>
    public sealed class ChatResponse
    {
        /// <summary>The generated text from the chat provider.</summary>
        public string? Text { get; set; }
        /// <summary>The session ID associated with the chat conversation.</summary>
        public string? SessionId { get; set; }
    }
}

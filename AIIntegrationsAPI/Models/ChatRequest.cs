using Swashbuckle.AspNetCore.Annotations;
using System.Collections.Generic;

namespace AIIntegrationsAPI.Models
{
    /// <summary>
    /// Represents a request to the chat provider, containing the prompt/messages context to generate a response for.
    /// </summary>
    public sealed class ChatRequest
    {
        //// For one-off prompts
        //public string? Prompt { get; set; }

        public List<ChatMessage> Messages { get; set; } = new();

        // Optional provider override (Claude/OpenAI)
        public string? Provider { get; set; }
    }
}

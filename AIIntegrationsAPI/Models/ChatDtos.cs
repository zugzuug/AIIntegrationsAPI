//using System.ComponentModel.DataAnnotations;

//namespace AIIntegrationsAPI.Models
//{
//    /// <summary>
//    /// Represents a request to the chat provider, containing the prompt to generate a response for.
//    /// </summary>
//    public sealed class ChatRequest
//    {
//        /// <summary>
//        /// The input prompt or query for the chat provider.
//        /// </summary>
//        [Required]
//        public string Prompt { get; set; } = string.Empty;
//        // Optional per-request override: "Claude" or "OpenAI"
//        //public string? Provider { get; set; }
//    }

//    /// <summary>
//    /// Represents the response from the chat provider, containing the generated text.
//    /// </summary>
//    public sealed class ChatResponse
//    {
//        /// <summary>
//        /// The generated response text from the chat provider.
//        /// </summary>
//        public string Text { get; set; } = string.Empty;
//    }
//}

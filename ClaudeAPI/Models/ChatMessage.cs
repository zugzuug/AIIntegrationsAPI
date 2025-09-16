namespace AIIntegrationsAPI.Models
{
    public sealed class ChatMessage
    {
        /// <summary>
        /// The role of the message sender.
        /// Valid values are "system", "user", or "assistant".
        /// </summary>
        public string Role { get; set; } = string.Empty;

        /// <summary>
        /// The content of the message.
        /// This is the actual text sent by the sender.
        /// </summary>
        public string Content { get; set; } = string.Empty;
    }
}

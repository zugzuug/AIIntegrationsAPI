using System.Collections.Generic;

namespace AIIntegrationsAPI.Options
{
    /// <summary>Options for API key management, including header name, owner key, and guest keys.</summary>
    public sealed class ApiKeyOptions
    {
        /// <summary>The name of the header used for API key authentication.</summary>
        public string HeaderName { get; set; } = "x-api-key";
        /// <summary>The primary owner API key.</summary>
        public string OwnerKey { get; set; } = string.Empty;
        /// <summary>List of guest API keys with associated metadata.</summary>
        public List<GuestKey> Guests { get; set; } = new();
    }
}

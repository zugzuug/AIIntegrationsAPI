using System;

namespace AIIntegrationsAPI.Options
{
    /// <summary>Represents a guest API key with expiration and label metadata.</summary>
    public sealed class GuestKey
    {
        /// <summary>The guest API key value.</summary>
        public string Key { get; set; } = string.Empty;
        /// <summary>The UTC expiration date and time for the guest key.</summary>
        public DateTime ExpiresUtc { get; set; }
        /// <summary>A label describing the guest key.</summary>
        public string Label { get; set; } = string.Empty;
    }
}

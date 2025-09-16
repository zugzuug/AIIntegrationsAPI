using System.Collections.Generic;

namespace AIIntegrationsAPI.Options
{
    /// <summary>
    /// Generic AI options (provider-agnostic). Keep secrets (ApiKey) out of source; bind via env/app settings.
    /// </summary>
    public sealed class AiOptions
    {
        /// <summary>Default provider name when not specified per request (e.g., "Claude" or "OpenAI").</summary>
        public string Provider { get; set; } = "Claude";

        /// <summary>Per-provider settings, keyed by provider name.</summary>
        public Dictionary<string, ProviderOptions> Providers { get; set; } = new();
    }

}

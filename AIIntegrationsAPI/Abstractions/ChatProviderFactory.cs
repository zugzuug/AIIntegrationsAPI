using System;
using System.Net.Http;
using AIIntegrationsAPI.Abstractions;
using AIIntegrationsAPI.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AIIntegrationsAPI.Providers
{
    public sealed class ChatProviderFactory : IChatProviderFactory
    {
        private readonly IHttpClientFactory _httpFactory;
        private readonly IOptions<AiOptions> _aiOptions;
        private readonly ILoggerFactory _loggerFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChatProviderFactory"/> class.
        /// </summary>
        /// <param name="httpFactory">
        /// Used to create named <see cref="HttpClient"/> instances for each provider.
        /// Ensures proper HTTP configuration and resource management per provider.
        /// </param>
        /// <param name="aiOptions">
        /// Provides access to AI configuration, including default provider and per-provider settings.
        /// Bound from application/environment settings for flexibility and security.
        /// </param>
        /// <param name="loggerFactory">
        /// Used to create type-specific loggers for each chat provider implementation.
        /// Enables diagnostic and error logging per provider.
        /// </param>
        public ChatProviderFactory(
            IHttpClientFactory httpFactory,
            IOptions<AiOptions> aiOptions,
            ILoggerFactory loggerFactory)
        {
            _httpFactory = httpFactory ?? throw new ArgumentNullException(nameof(httpFactory));
            _aiOptions = aiOptions ?? throw new ArgumentNullException(nameof(aiOptions));
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        }

        public IChatProvider Create(string providerName)
        {

#if DEBUG
            // In development, treat "string" as an empty provider name for easier local testing.
            providerName = (providerName == "string") ? string.Empty : providerName;
#endif
            var options = _aiOptions.Value;

            // Choose requested provider or fall back to default from config
            var key = string.IsNullOrWhiteSpace(providerName) ? options.Provider : providerName;

            // Try exact match first
            if (!options.Providers.TryGetValue(key, out var providerOptions))
            {
                // Case-insensitive fallback
                foreach (var kv in options.Providers)
                {
                    if (string.Equals(kv.Key, key, StringComparison.OrdinalIgnoreCase))
                    {
                        key = kv.Key;              // normalize to the canonical key casing
                        providerOptions = kv.Value;
                        break;
                    }
                }
            }

            if (providerOptions is null)
            {
                var configured = string.Join(", ", options.Providers.Keys);
                throw new InvalidOperationException(
                    $"Unknown AI provider '{key}'. Configured providers: [{configured}]. " +
                    "Check Ai:Providers in configuration.");
            }

            // Create a named HttpClient for this provider (registered in Program.cs)
            var http = _httpFactory.CreateClient(key);

            // Instantiate the concrete provider
            if (string.Equals(key, "Claude", StringComparison.OrdinalIgnoreCase))
            {
                return new AIIntegrationsAPI.Providers.Claude.ClaudeChatProvider(
                    http,
                    providerOptions,
                    _loggerFactory.CreateLogger<AIIntegrationsAPI.Providers.Claude.ClaudeChatProvider>());
            }

            if (string.Equals(key, "OpenAI", StringComparison.OrdinalIgnoreCase))
            {
                return new AIIntegrationsAPI.Providers.OpenAI.OpenAIChatProvider(
                    http,
                    providerOptions,
                    _loggerFactory.CreateLogger<AIIntegrationsAPI.Providers.OpenAI.OpenAIChatProvider>());
            }

            // If you add more providers later, extend the branches above.
            throw new InvalidOperationException(
                $"Provider '{key}' has no implementation registered in ChatProviderFactory.");
        }

    }
}

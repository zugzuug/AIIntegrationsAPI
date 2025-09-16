namespace AIIntegrationsAPI.Abstractions
{
    /// <summary>
    /// Factory interface for creating provider-agnostic chat providers.
    /// Implementations should return an appropriate IChatProvider instance
    /// based on the specified provider name (e.g., Claude, OpenAI).
    /// </summary>
    public interface IChatProviderFactory
    {
        /// <summary>Creates an IChatProvider instance for the given provider name.</summary>
        /// <param name="providerName">The name of the chat provider to instantiate (e.g., "Claude", "OpenAI").</param>
        /// <returns>An IChatProvider implementation corresponding to the specified provider name.</returns>
        IChatProvider Create(string providerName);
    }
}

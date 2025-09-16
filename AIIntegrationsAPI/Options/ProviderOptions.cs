/// <summary>Configuration options for a chat provider, including endpoint, credentials, model, and limits.</summary>
public sealed class ProviderOptions
{
    /// <summary>Base URL of the provider API (e.g., https://api.anthropic.com or https://api.openai.com).</summary>
    public string BaseUrl { get; set; } = string.Empty;
    /// <summary>API key for authentication, typically supplied via environment or app settings.</summary>
    public string ApiKey { get; set; } = string.Empty;
    /// <summary>Model name to use (e.g., claude-sonnet-4-20250514 or gpt-4o-mini).</summary>
    public string Model { get; set; } = string.Empty;
    /// <summary>API version, required by some providers (e.g., Anthropic), ignored by others.</summary>
    public string ApiVersion { get; set; } = "";
    /// <summary>Maximum number of tokens allowed in a response.</summary>
    public int MaxTokens { get; set; } = 1024;
}
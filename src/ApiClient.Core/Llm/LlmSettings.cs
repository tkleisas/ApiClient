namespace ApiClient.Core.Llm;

/// <summary>
/// Settings for the built-in OpenAI-compatible LLM service, stored as part of
/// <see cref="ApiClient.Core.Model.AppSettings"/>. Hosts that inject their own
/// <see cref="ILlmService"/> never read these.
/// </summary>
public record LlmSettings
{
    /// <summary>Whether AI features are enabled.</summary>
    public bool Enabled { get; init; }

    /// <summary>OpenAI-compatible base URL (e.g. <c>https://api.openai.com/v1</c>, LM Studio, Ollama).</summary>
    public string Endpoint { get; init; } = "https://api.openai.com/v1";

    /// <summary>API key / auth token. Empty for local models.</summary>
    public string ApiKey { get; init; } = string.Empty;

    /// <summary>Model ID sent in API requests.</summary>
    public string Model { get; init; } = "gpt-4o-mini";

    /// <summary>Sampling temperature (0.0–2.0).</summary>
    public double Temperature { get; init; } = 0.2;
}

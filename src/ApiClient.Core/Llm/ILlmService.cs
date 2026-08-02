using System.Threading;
using System.Threading.Tasks;

namespace ApiClient.Core.Llm;

/// <summary>
/// Minimal LLM abstraction for AI-assisted request building and response analysis.
/// The core ships a built-in OpenAI-compatible implementation
/// (<see cref="OpenAiCompatibleLlmService"/>); hosts embedding the workspace
/// (e.g. NVS) can inject their own implementation to reuse the host's LLM configuration.
/// </summary>
public interface ILlmService
{
    /// <summary>Whether the service has enough configuration to send requests.</summary>
    bool IsConfigured { get; }

    /// <summary>
    /// Sends a single system + user prompt pair and returns the assistant's reply.
    /// </summary>
    Task<string> ChatAsync(string systemPrompt, string userPrompt, CancellationToken ct = default);
}

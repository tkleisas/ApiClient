using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ApiClient.Core.Llm;

/// <summary>
/// <see cref="ILlmService"/> over an OpenAI-compatible chat completions endpoint.
/// Works with OpenAI, OpenRouter, DeepSeek, Ollama, LM Studio and similar providers.
/// </summary>
public class OpenAiCompatibleLlmService : ILlmService
{
    private readonly LlmSettings _settings;
    private readonly HttpClient? _client;

    /// <summary>Creates the service from settings.</summary>
    public OpenAiCompatibleLlmService(LlmSettings settings)
    {
        _settings = settings;
    }

    /// <summary>Creates the service with an explicit HTTP client (used for testing).</summary>
    public OpenAiCompatibleLlmService(LlmSettings settings, HttpClient client)
    {
        _settings = settings;
        _client = client;
    }

    /// <inheritdoc/>
    public bool IsConfigured =>
        _settings.Enabled
        && !string.IsNullOrWhiteSpace(_settings.Endpoint)
        && !string.IsNullOrWhiteSpace(_settings.Model);

    /// <inheritdoc/>
    public async Task<string> ChatAsync(string systemPrompt, string userPrompt, CancellationToken ct = default)
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException("LLM service is not configured. Open Settings to set an endpoint and model.");
        }

        var requestBody = new
        {
            model = _settings.Model,
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            },
            temperature = _settings.Temperature,
            stream = false
        };

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            BuildCompletionsUrl(_settings.Endpoint))
        {
            Content = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json")
        };

        if (!string.IsNullOrWhiteSpace(_settings.ApiKey))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.ApiKey);
        }

        try
        {
            var client = _client ?? CreateClient();
            using var response = await client.SendAsync(request, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var detail = (await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false)).Trim();
                if (detail.Length > 500)
                {
                    detail = detail[..500] + "…";
                }
                throw new InvalidOperationException(
                    string.IsNullOrEmpty(detail)
                        ? $"LLM request failed: {(int)response.StatusCode} {response.ReasonPhrase}"
                        : $"LLM request failed: {(int)response.StatusCode} {response.ReasonPhrase} — {detail}");
            }

            var responseJson = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            return ExtractContent(responseJson);
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            throw new InvalidOperationException("LLM request timed out.");
        }
    }

    internal static string ExtractContent(string responseJson)
    {
        using var doc = JsonDocument.Parse(responseJson);
        var choices = doc.RootElement.GetProperty("choices");
        if (choices.GetArrayLength() == 0)
        {
            return string.Empty;
        }

        var message = choices[0].GetProperty("message");
        return message.TryGetProperty("content", out var content)
            ? content.GetString() ?? string.Empty
            : string.Empty;
    }

    internal static string BuildCompletionsUrl(string endpoint)
    {
        var baseUrl = endpoint.TrimEnd('/');
        return baseUrl.EndsWith("/chat/completions", StringComparison.OrdinalIgnoreCase)
            ? baseUrl
            : $"{baseUrl}/chat/completions";
    }

    private static HttpClient CreateClient() =>
        new() { Timeout = TimeSpan.FromSeconds(60) };
}

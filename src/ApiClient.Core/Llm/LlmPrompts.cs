using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using ApiClient.Core.Model;

namespace ApiClient.Core.Llm;

/// <summary>
/// Pure prompt builders for the AI features: natural-language → request generation,
/// response analysis, and post-response test script generation.
/// </summary>
public static class LlmPrompts
{
    /// <summary>Maximum response body characters sent for analysis.</summary>
    public const int MaxAnalysisBodyChars = 8000;

    /// <summary>Translate a natural-language description into a request definition (strict JSON).</summary>
    public static (string System, string User) BuildRequestFromDescription(string description)
    {
        var system = """
            You are an HTTP API expert embedded in an API client application.
            Translate the user's description into an HTTP request.
            Reply with ONLY a JSON object (no markdown fences, no prose) of this exact shape:
            {"name":"short request name","method":"GET","url":"https://...","headers":{"Header-Name":"value"},"body":"request body text or empty string","bodyMediaType":"application/json"}
            Use a realistic example URL when the user does not specify one.
            Omit the "body" and "bodyMediaType" keys entirely for bodiless methods like GET.
            """;

        return (system, $"Description: {description}");
    }

    /// <summary>Summarize an HTTP response: meaning, notable headers, body patterns, problems.</summary>
    public static (string System, string User) BuildAnalyzeResponse(int statusCode, string headers, string body)
    {
        var system = """
            You are an HTTP API expert embedded in an API client application.
            Analyze the given HTTP response: what it means, whether it indicates success or a
            problem, notable headers, and patterns or anomalies in the body.
            Be concise — a short paragraph or a few bullets.
            """;

        var truncatedBody = body.Length > MaxAnalysisBodyChars
            ? body[..MaxAnalysisBodyChars] + "… (truncated)"
            : body;

        var user = $"""
            Status: {statusCode}

            Headers:
            {headers}

            Body:
            {truncatedBody}
            """;

        return (system, user);
    }

    /// <summary>Generate a post-response test script for the request, using the app's JS scripting API.</summary>
    public static (string System, string User) BuildTestScript(ApiRequest request)
    {
        var system = """
            You are an API testing expert embedded in an API client application.
            Write a post-response JavaScript test script for the given request.
            Available API:
              res.status            — HTTP status code (number)
              res.body              — response body text (use JSON.parse(res.body) for JSON)
              res.getHeader(name)   — response header value or null
              bru.setVar(name, v)   — store a variable for later requests
              test(name, fn)        — register a test; fn throws on failure
              expect(actual).toBe(e) / .toEqual(e) / .toContain(s)
            Reply with ONLY the JavaScript code — no markdown fences, no prose.
            Cover: status code, content type, and 1–3 assertions on the expected body shape.
            """;

        var user = new StringBuilder()
            .Append("Request: ").Append(request.Method).Append(' ').AppendLine(request.Url)
            .Append("Request body: ").AppendLine(
                request.Body.Type == BodyType.Raw ? request.Body.Text ?? "(empty)" : "(none)")
            .ToString();

        return (system, user);
    }

    /// <summary>
    /// Parses the JSON object produced for <see cref="BuildRequestFromDescription"/> into an
    /// <see cref="ApiRequest"/>. Tolerates ```json fences around the payload.
    /// </summary>
    /// <exception cref="FormatException">The text is not a usable request definition.</exception>
    public static ApiRequest ParseGeneratedRequest(string llmText)
    {
        var json = ExtractCode(llmText);

        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(json);
        }
        catch (JsonException ex)
        {
            throw new FormatException("The AI reply is not valid JSON.", ex);
        }

        using (doc)
        {
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                throw new FormatException("The AI reply is not a JSON object.");
            }

            var url = GetString(root, "url");
            if (string.IsNullOrWhiteSpace(url))
            {
                throw new FormatException("The AI reply contains no URL.");
            }

            var headers = new List<KeyValueItem>();
            if (root.TryGetProperty("headers", out var headersEl) && headersEl.ValueKind == JsonValueKind.Object)
            {
                foreach (var header in headersEl.EnumerateObject())
                {
                    headers.Add(new KeyValueItem(header.Name, header.Value.GetString() ?? string.Empty));
                }
            }

            var bodyText = GetString(root, "body");
            var body = string.IsNullOrEmpty(bodyText)
                ? new RequestBody()
                : new RequestBody
                {
                    Type = BodyType.Raw,
                    MediaType = GetString(root, "bodyMediaType") ?? "application/json",
                    Text = bodyText
                };

            return new ApiRequest
            {
                Name = GetString(root, "name") ?? "AI request",
                Method = GetString(root, "method")?.ToUpperInvariant() ?? "GET",
                Url = url,
                Headers = headers,
                Body = body,
            };
        }
    }

    /// <summary>
    /// Extracts code from an LLM reply: the first fenced code block when present,
    /// otherwise the whole trimmed text.
    /// </summary>
    public static string ExtractCode(string llmText)
    {
        if (string.IsNullOrWhiteSpace(llmText))
        {
            return string.Empty;
        }

        var text = llmText;
        var fenceStart = text.IndexOf("```", StringComparison.Ordinal);
        if (fenceStart >= 0)
        {
            var contentStart = text.IndexOf('\n', fenceStart);
            if (contentStart >= 0)
            {
                var fenceEnd = text.IndexOf("```", contentStart, StringComparison.Ordinal);
                if (fenceEnd > contentStart)
                {
                    return text[(contentStart + 1)..fenceEnd].Trim();
                }
            }
        }

        return text.Trim();
    }

    private static string? GetString(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
}

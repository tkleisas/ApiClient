using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace ApiClient.Core.Scripting;

// These types are exposed to JavaScript, so their members are intentionally camelCase
// (req.url, bru.setVar, crypto.hmacSha256) to read naturally from scripts.

/// <summary>The outgoing request, exposed to pre-request scripts as <c>req</c> (mutable).</summary>
public sealed class ScriptRequest
{
    private readonly IDictionary<string, string> _headers;

    /// <summary>Creates the script request over a shared header dictionary (mutations are read back).</summary>
    public ScriptRequest(string url, string method, string body, IDictionary<string, string> headers)
    {
        this.url = url;
        this.method = method;
        this.body = body;
        _headers = headers;
    }

    /// <summary>The request URL.</summary>
    public string url { get; set; }

    /// <summary>The HTTP method.</summary>
    public string method { get; set; }

    /// <summary>The request body text.</summary>
    public string body { get; set; }

    /// <summary>Sets a request header.</summary>
    public void setHeader(string name, string value) => _headers[name] = value;

    /// <summary>Gets a request header value, or null.</summary>
    public string? getHeader(string name) => _headers.TryGetValue(name, out var value) ? value : null;
}

/// <summary>The response, exposed to post-response scripts as <c>res</c>.</summary>
public sealed class ScriptResponse
{
    private readonly IDictionary<string, string> _headers;

    /// <summary>Creates the script response.</summary>
    public ScriptResponse(int status, string body, IDictionary<string, string> headers)
    {
        this.status = status;
        this.body = body;
        _headers = headers;
    }

    /// <summary>The HTTP status code.</summary>
    public int status { get; }

    /// <summary>The response body text (use <c>JSON.parse(res.body)</c> for JSON).</summary>
    public string body { get; }

    /// <summary>Gets a response header value, or null.</summary>
    public string? getHeader(string name) => _headers.TryGetValue(name, out var value) ? value : null;
}

/// <summary>Variable access, exposed to scripts as <c>bru</c>.</summary>
public sealed class ScriptVars
{
    private readonly IDictionary<string, string> _vars;

    /// <summary>Creates variable access over a shared dictionary (set values are read back).</summary>
    public ScriptVars(IDictionary<string, string> vars) => _vars = vars;

    /// <summary>Gets a variable value, or null.</summary>
    public string? getVar(string name) => _vars.TryGetValue(name, out var value) ? value : null;

    /// <summary>Sets a variable (e.g. to chain a token into later requests).</summary>
    public void setVar(string name, object? value) => _vars[name] = value?.ToString() ?? string.Empty;
}

/// <summary>Hashing/encoding helpers, exposed to scripts as <c>crypto</c> (e.g. for request signing).</summary>
public sealed class CryptoApi
{
    /// <summary>HMAC-SHA256 of <paramref name="message"/> with <paramref name="key"/>, lowercase hex.</summary>
    public string hmacSha256(string message, string key)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
        return Convert.ToHexStringLower(hmac.ComputeHash(Encoding.UTF8.GetBytes(message)));
    }

    /// <summary>SHA-256 of <paramref name="message"/>, lowercase hex.</summary>
    public string sha256(string message) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(message)));

    /// <summary>MD5 of <paramref name="message"/>, lowercase hex.</summary>
    public string md5(string message) => Convert.ToHexStringLower(MD5.HashData(Encoding.UTF8.GetBytes(message)));

    /// <summary>Base64-encodes a UTF-8 string.</summary>
    public string base64Encode(string text) => Convert.ToBase64String(Encoding.UTF8.GetBytes(text));

    /// <summary>Decodes a Base64 string to UTF-8.</summary>
    public string base64Decode(string text) => Encoding.UTF8.GetString(Convert.FromBase64String(text));
}

using ApiClient.Core.Model;

namespace ApiClient.Core.CodeGen;

/// <summary>Whether a generator emits code that <em>calls</em> an API or code that <em>implements</em> one.</summary>
public enum CodeGenScenario
{
    /// <summary>Client code that sends the request (e.g. an <c>HttpClient</c> snippet).</summary>
    Client,

    /// <summary>Server code that implements the endpoint (e.g. a contract or handler stub).</summary>
    Server,
}

/// <summary>
/// Generates source code from an <see cref="ApiRequest"/>. Generators are pluggable so
/// that new languages, frameworks, and both client and server scenarios can be added
/// without changing existing code.
/// </summary>
public interface ICodeGenerator
{
    /// <summary>A stable, machine-friendly identifier, e.g. <c>"csharp-httpclient"</c>.</summary>
    string Id { get; }

    /// <summary>A human-friendly name for menus, e.g. <c>"C# — HttpClient"</c>.</summary>
    string DisplayName { get; }

    /// <summary>Whether this generator targets the client or server scenario.</summary>
    CodeGenScenario Scenario { get; }

    /// <summary>Generates source code that corresponds to <paramref name="request"/>.</summary>
    string Generate(ApiRequest request);
}

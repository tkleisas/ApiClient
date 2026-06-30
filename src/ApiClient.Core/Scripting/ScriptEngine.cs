using System;
using System.Collections.Generic;
using System.Text.Json;
using Jint;
using Jint.Runtime;

namespace ApiClient.Core.Scripting;

/// <summary>The result of one <c>test(...)</c> assertion in a script.</summary>
public record TestResult(string Name, bool Passed, string? Message);

/// <summary>The outcome of running a script: any test results, plus a script error if it threw.</summary>
public record ScriptResult(IReadOnlyList<TestResult> Tests, string? Error)
{
    /// <summary>An empty result (no tests, no error) — used when there is no script.</summary>
    public static ScriptResult Empty { get; } = new ScriptResult([], null);
}

/// <summary>
/// Runs request JavaScript using the embedded <see href="https://github.com/sebastienros/jint">Jint</see>
/// engine. Scripts get a small Bruno-flavoured API: <c>req</c>, <c>res</c>, <c>bru</c> (variables),
/// <c>crypto</c> (signing helpers), and <c>test</c>/<c>expect</c> for assertions.
/// </summary>
public sealed class ScriptEngine
{
    private static readonly JsonSerializerOptions TestJson = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

    // Defines test() and expect() in the script scope and collects results into __tests.
    private const string Prelude = """
        var __tests = [];
        function test(name, fn) {
            try { fn(); __tests.push({ name: name, passed: true, message: null }); }
            catch (e) { __tests.push({ name: name, passed: false, message: String((e && e.message) || e) }); }
        }
        function expect(actual) {
            return {
                toBe: function (e) { if (actual !== e) throw new Error('expected ' + e + ' but got ' + actual); },
                toEqual: function (e) { if (actual != e) throw new Error('expected ' + e + ' but got ' + actual); },
                toContain: function (s) { if (String(actual).indexOf(s) < 0) throw new Error('expected to contain ' + s); }
            };
        }
        """;

    /// <summary>Runs a pre-request script, letting it mutate <paramref name="request"/> and <paramref name="variables"/>.</summary>
    public ScriptResult RunPreRequest(string script, ScriptRequest request, IDictionary<string, string> variables)
    {
        if (string.IsNullOrWhiteSpace(script))
            return ScriptResult.Empty;

        var engine = CreateEngine();
        engine.SetValue("req", request);
        engine.SetValue("bru", new ScriptVars(variables));
        engine.SetValue("crypto", new CryptoApi());
        return Execute(engine, script);
    }

    /// <summary>Runs a post-response script with access to the request, response, and variables.</summary>
    public ScriptResult RunPostResponse(string script, ScriptRequest request, ScriptResponse response, IDictionary<string, string> variables)
    {
        if (string.IsNullOrWhiteSpace(script))
            return ScriptResult.Empty;

        var engine = CreateEngine();
        engine.SetValue("req", request);
        engine.SetValue("res", response);
        engine.SetValue("bru", new ScriptVars(variables));
        engine.SetValue("crypto", new CryptoApi());
        return Execute(engine, script);
    }

    private static Engine CreateEngine() => new Engine(options => options
        .TimeoutInterval(TimeSpan.FromSeconds(5))
        .LimitRecursion(64));

    private static ScriptResult Execute(Engine engine, string script)
    {
        try
        {
            engine.Execute(Prelude);
            engine.Execute(script);
            var testsJson = engine.Evaluate("JSON.stringify(__tests)").AsString();
            var tests = JsonSerializer.Deserialize<List<TestResult>>(testsJson, TestJson) ?? [];
            return new ScriptResult(tests, null);
        }
        catch (JavaScriptException ex)
        {
            return new ScriptResult([], ex.Message);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            return new ScriptResult([], ex.Message);
        }
    }
}

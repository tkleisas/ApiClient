using System.Collections.Generic;
using System.Linq;
using ApiClient.Core.Scripting;
using Xunit;

namespace ApiClient.Core.Tests;

public class ScriptEngineTests
{
    private static ScriptRequest Request(IDictionary<string, string>? headers = null)
        => new ScriptRequest("https://h/api", "GET", "", headers ?? new Dictionary<string, string>());

    [Fact]
    public void Pre_request_script_can_modify_the_request()
    {
        var headers = new Dictionary<string, string>();
        var request = new ScriptRequest("https://h/api", "GET", "", headers);

        var result = new ScriptEngine().RunPreRequest(
            "req.url = 'https://h/v2'; req.method = 'POST'; req.setHeader('X-Trace', '1');",
            request, new Dictionary<string, string>());

        Assert.Null(result.Error);
        Assert.Equal("https://h/v2", request.url);
        Assert.Equal("POST", request.method);
        Assert.Equal("1", headers["X-Trace"]);
    }

    [Fact]
    public void Pre_request_script_can_set_variables()
    {
        var vars = new Dictionary<string, string>();

        new ScriptEngine().RunPreRequest("bru.setVar('token', 'abc' + 123);", Request(), vars);

        Assert.Equal("abc123", vars["token"]);
    }

    [Fact]
    public void Post_response_script_extracts_a_value_into_a_variable()
    {
        var vars = new Dictionary<string, string>();
        var response = new ScriptResponse(200, "{\"token\":\"xyz\"}", new Dictionary<string, string>());

        new ScriptEngine().RunPostResponse(
            "var data = JSON.parse(res.body); bru.setVar('authToken', data.token);",
            Request(), response, vars);

        Assert.Equal("xyz", vars["authToken"]);
    }

    [Fact]
    public void Post_response_assertions_report_pass_and_fail()
    {
        var response = new ScriptResponse(200, "{\"ok\":true}", new Dictionary<string, string>());

        var result = new ScriptEngine().RunPostResponse(
            """
            test('status is 200', function () { expect(res.status).toBe(200); });
            test('body has ok', function () { expect(res.body).toContain('"ok":true'); });
            test('this fails', function () { expect(res.status).toBe(500); });
            """,
            Request(), response, new Dictionary<string, string>());

        Assert.Null(result.Error);
        Assert.Equal(3, result.Tests.Count);
        Assert.True(result.Tests.Single(t => t.Name == "status is 200").Passed);
        Assert.False(result.Tests.Single(t => t.Name == "this fails").Passed);
    }

    [Fact]
    public void Crypto_helper_computes_hmac()
    {
        var vars = new Dictionary<string, string>();

        new ScriptEngine().RunPreRequest("bru.setVar('sig', crypto.hmacSha256('message', 'key'));", Request(), vars);

        // Known HMAC-SHA256("message","key") lowercase hex.
        Assert.Equal("6e9ef29b75fffc5b7abae527d58fdadb2fe42e7219011976917343065f58ed4a", vars["sig"]);
    }

    [Fact]
    public void Script_error_is_captured_not_thrown()
    {
        var result = new ScriptEngine().RunPreRequest("this is not valid js !!!", Request(), new Dictionary<string, string>());

        Assert.NotNull(result.Error);
    }

    [Fact]
    public void Empty_script_is_a_no_op()
    {
        var result = new ScriptEngine().RunPreRequest("", Request(), new Dictionary<string, string>());

        Assert.Null(result.Error);
        Assert.Empty(result.Tests);
    }
}

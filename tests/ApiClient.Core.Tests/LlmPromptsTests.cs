using System;
using System.Linq;
using ApiClient.Core.Llm;
using ApiClient.Core.Model;
using Xunit;

namespace ApiClient.Core.Tests;

public class LlmPromptsTests
{
    [Fact]
    public void ParseGeneratedRequest_FullJson_MapsAllFields()
    {
        var json = """
            {"name":"Create user","method":"post","url":"https://api.example.com/users",
             "headers":{"Content-Type":"application/json","X-Key":"abc"},
             "body":"{\"name\":\"john\"}","bodyMediaType":"application/json"}
            """;

        var request = LlmPrompts.ParseGeneratedRequest(json);

        Assert.Equal("Create user", request.Name);
        Assert.Equal("POST", request.Method);
        Assert.Equal("https://api.example.com/users", request.Url);
        Assert.Equal(2, request.Headers.Count);
        Assert.Equal("X-Key", request.Headers[1].Name);
        Assert.Equal(BodyType.Raw, request.Body.Type);
        Assert.Equal("application/json", request.Body.MediaType);
        Assert.Equal("{\"name\":\"john\"}", request.Body.Text);
    }

    [Fact]
    public void ParseGeneratedRequest_FencedJson_Parses()
    {
        var text = "Here is the request:\n```json\n{\"url\":\"https://api.example.com/items\"}\n```\nDone.";

        var request = LlmPrompts.ParseGeneratedRequest(text);

        Assert.Equal("https://api.example.com/items", request.Url);
        Assert.Equal("GET", request.Method);
        Assert.Equal(BodyType.None, request.Body.Type);
    }

    [Fact]
    public void ParseGeneratedRequest_NoUrl_Throws()
    {
        Assert.Throws<FormatException>(() => LlmPrompts.ParseGeneratedRequest("{\"method\":\"GET\"}"));
    }

    [Fact]
    public void ParseGeneratedRequest_NotJson_Throws()
    {
        Assert.Throws<FormatException>(() => LlmPrompts.ParseGeneratedRequest("sorry, I cannot help"));
    }

    [Fact]
    public void BuildRequestFromDescription_IncludesDescription()
    {
        var (system, user) = LlmPrompts.BuildRequestFromDescription("list users");

        Assert.Contains("JSON", system);
        Assert.Contains("list users", user);
    }

    [Fact]
    public void BuildAnalyzeResponse_TruncatesLongBodies()
    {
        var body = new string('x', LlmPrompts.MaxAnalysisBodyChars + 100);

        var (_, user) = LlmPrompts.BuildAnalyzeResponse(200, "Content-Type: application/json", body);

        Assert.Contains("(truncated)", user);
        Assert.DoesNotContain(body, user);
    }

    [Fact]
    public void BuildTestScript_DocumentsScriptingApi()
    {
        var request = new ApiRequest { Name = "r", Method = "GET", Url = "https://api.example.com" };

        var (system, user) = LlmPrompts.BuildTestScript(request);

        Assert.Contains("res.status", system);
        Assert.Contains("test(name, fn)", system);
        Assert.Contains("GET https://api.example.com", user);
    }

    [Fact]
    public void ExtractCode_FencedBlock_ReturnsContents()
    {
        Assert.Equal("test('ok', () => {});", LlmPrompts.ExtractCode("```javascript\ntest('ok', () => {});\n```"));
    }

    [Fact]
    public void ExtractCode_NoFence_ReturnsTrimmedText()
    {
        Assert.Equal("res.status", LlmPrompts.ExtractCode("  res.status  "));
    }
}

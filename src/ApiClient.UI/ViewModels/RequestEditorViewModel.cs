using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using ApiClient.Core.CodeGen;
using ApiClient.Core.Hosting;
using ApiClient.Core.Http;
using ApiClient.Core.Json;
using ApiClient.Core.Model;
using ApiClient.Core.Scripting;
using ApiClient.Core.Variables;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ApiClient.UI.ViewModels;

/// <summary>One editable header row in the request editor.</summary>
public partial class HeaderRowViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _enabled = true;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _value = string.Empty;
}

/// <summary>
/// Edits a single request (method, URL, headers, body), sends it through the
/// <see cref="ApiClient.Core"/> engine, and shows the response. Deliberately thin — all
/// real work happens in <see cref="RequestExecutor"/>. This view model backs the embeddable
/// <c>ApiClientView</c>, so it carries no window/desktop assumptions; the surrounding host
/// is reached only through <see cref="IHostServices"/>.
/// </summary>
public partial class RequestEditorViewModel : ViewModelBase
{
    private static readonly IReadOnlyDictionary<string, string> NoVariables = new Dictionary<string, string>();

    private readonly RequestExecutor _executor;
    private readonly IHostServices _host;
    private readonly ScriptEngine _scriptEngine = new ScriptEngine();

    // Variables set by scripts (bru.setVar) — persist across requests for chaining.
    private readonly Dictionary<string, string> _extracted = new Dictionary<string, string>();

    /// <summary>Design-time / default constructor: wires the real HTTP engine and standalone host services.</summary>
    public RequestEditorViewModel()
        : this(
            new RequestExecutor(HttpRequestFactory.CreateDefault(), new HttpClientSender(new HttpClient())),
            new StandaloneHostServices())
    {
    }

    /// <summary>Creates the view model with an explicit executor and host services (used for embedding/testing).</summary>
    public RequestEditorViewModel(RequestExecutor executor, IHostServices host)
    {
        _executor = executor;
        _host = host;
        Headers.Add(new HeaderRowViewModel { Name = "Accept", Value = "application/json" });
        SelectedGenerator = Generators[0];
    }

    /// <summary>The name of the request being edited (preserved so saves overwrite the right file).</summary>
    public string RequestName { get; set; } = "Untitled";

    /// <summary>Variables (from the active environment) used to resolve <c>{{tokens}}</c> when sending.</summary>
    public IReadOnlyDictionary<string, string> Variables { get; set; } = NoVariables;

    /// <summary>Invoked after each successful send so a host can record it to history.</summary>
    public Action<HistoryEntry>? RequestRecorded { get; set; }

    /// <summary>The HTTP methods offered in the method drop-down.</summary>
    public string[] Methods { get; } = ["GET", "POST", "PUT", "PATCH", "DELETE", "HEAD", "OPTIONS"];

    /// <summary>Editable request headers.</summary>
    public ObservableCollection<HeaderRowViewModel> Headers { get; } = [];

    /// <summary>Available code generators (client and server scenarios).</summary>
    public IReadOnlyList<ICodeGenerator> Generators { get; } =
    [
        CSharpHttpClientGenerator.CreateDefault(),
        new CSharpMinimalApiGenerator(),
    ];

    /// <summary>The authentication schemes offered in the Auth tab.</summary>
    public AuthType[] AuthTypes { get; } = [AuthType.None, AuthType.Bearer, AuthType.Basic, AuthType.ApiKey];

    /// <summary>Where an API key can be placed.</summary>
    public ApiKeyLocation[] ApiKeyLocations { get; } = [ApiKeyLocation.Header, ApiKeyLocation.Query];

    [ObservableProperty]
    private string _method = "GET";

    [ObservableProperty]
    private string _url = "https://httpbin.org/get";

    [ObservableProperty]
    private string _requestBody = string.Empty;

    [ObservableProperty]
    private string _bodyMediaType = "application/json";

    [ObservableProperty]
    private bool _isSending;

    [ObservableProperty]
    private string _responseSummary = string.Empty;

    [ObservableProperty]
    private string _responseHeaders = string.Empty;

    [ObservableProperty]
    private string _responseBody = string.Empty;

    [ObservableProperty]
    private string _responsePretty = string.Empty;

    [ObservableProperty]
    private bool _responseIsJson;

    [ObservableProperty]
    private bool _wordWrap;

    [ObservableProperty]
    private string _selectedResponseView = "Pretty";

    /// <summary>The response body view modes.</summary>
    public string[] ResponseViews { get; } = ["Pretty", "Raw", "Tree"];

    /// <summary>The parsed JSON response, for the expandable color-coded tree (single root, if any).</summary>
    public ObservableCollection<JsonTreeNode> ResponseTree { get; } = [];

    /// <summary>Whether the Pretty view is selected.</summary>
    public bool IsPrettyView => SelectedResponseView == "Pretty";

    /// <summary>Whether the Raw view is selected.</summary>
    public bool IsRawView => SelectedResponseView == "Raw";

    /// <summary>Whether the Tree view is selected.</summary>
    public bool IsTreeView => SelectedResponseView == "Tree";

    /// <summary>Whether a text-based view (Pretty or Raw) is selected — controls word-wrap visibility.</summary>
    public bool IsTextResponseView => IsPrettyView || IsRawView;

    partial void OnSelectedResponseViewChanged(string value)
    {
        OnPropertyChanged(nameof(IsPrettyView));
        OnPropertyChanged(nameof(IsRawView));
        OnPropertyChanged(nameof(IsTreeView));
        OnPropertyChanged(nameof(IsTextResponseView));
    }

    [ObservableProperty]
    private ICodeGenerator? _selectedGenerator;

    [ObservableProperty]
    private string _generatedCode = string.Empty;

    [ObservableProperty]
    private string _preRequestScript = string.Empty;

    [ObservableProperty]
    private string _postResponseScript = string.Empty;

    [ObservableProperty]
    private string _scriptError = string.Empty;

    /// <summary>Results of post/pre-response <c>test(...)</c> assertions from the last send.</summary>
    public ObservableCollection<TestResult> TestResults { get; } = [];

    [ObservableProperty]
    private AuthType _selectedAuthType;

    [ObservableProperty]
    private string _authToken = string.Empty;

    [ObservableProperty]
    private string _authUsername = string.Empty;

    [ObservableProperty]
    private string _authPassword = string.Empty;

    [ObservableProperty]
    private string _authKeyName = string.Empty;

    [ObservableProperty]
    private string _authKeyValue = string.Empty;

    [ObservableProperty]
    private ApiKeyLocation _authKeyLocation;

    /// <summary>Whether the bearer-token fields should be shown.</summary>
    public bool IsBearerAuth => SelectedAuthType == AuthType.Bearer;

    /// <summary>Whether the basic-auth fields should be shown.</summary>
    public bool IsBasicAuth => SelectedAuthType == AuthType.Basic;

    /// <summary>Whether the API-key fields should be shown.</summary>
    public bool IsApiKeyAuth => SelectedAuthType == AuthType.ApiKey;

    partial void OnSelectedAuthTypeChanged(AuthType value)
    {
        OnPropertyChanged(nameof(IsBearerAuth));
        OnPropertyChanged(nameof(IsBasicAuth));
        OnPropertyChanged(nameof(IsApiKeyAuth));
    }

    [RelayCommand]
    private void GenerateCode()
    {
        if (SelectedGenerator is not null)
            GeneratedCode = SelectedGenerator.Generate(BuildRequest());
    }

    partial void OnSelectedGeneratorChanged(ICodeGenerator? value) => GenerateCode();

    [RelayCommand]
    private void AddHeader() => Headers.Add(new HeaderRowViewModel());

    [RelayCommand]
    private void RemoveHeader(HeaderRowViewModel row) => Headers.Remove(row);

    [RelayCommand(CanExecute = nameof(CanSend))]
    private async Task SendAsync()
    {
        IsSending = true;
        ResponseSummary = "Sending…";
        ResponseHeaders = string.Empty;
        ScriptError = string.Empty;
        TestResults.Clear();
        ShowResponseBody(string.Empty);

        try
        {
            var request = BuildRequest();

            // Effective variables: environment overlaid with script-extracted runtime values.
            var variables = new Dictionary<string, string>(Variables);
            foreach (var (key, value) in _extracted)
                variables[key] = value;

            request = RunPreRequest(request, variables);

            var response = await _executor.ExecuteAsync(request, variables);

            ResponseSummary =
                $"{response.StatusCode} {response.ReasonPhrase} · {response.Elapsed.TotalMilliseconds:F0} ms · {FormatSize(response.SizeBytes)}";
            ResponseHeaders = string.Join(Environment.NewLine, response.Headers.Select(h => $"{h.Name}: {h.Value}"));
            ShowResponseBody(response.Body);

            RunPostResponse(request, response, variables);
            PersistRuntimeVariables(variables);

            RequestRecorded?.Invoke(new HistoryEntry
            {
                Timestamp = DateTimeOffset.Now,
                Method = request.Method,
                Url = new VariableResolver().Resolve(request.Url, variables),
                Status = response.StatusCode,
                ElapsedMs = (long)response.Elapsed.TotalMilliseconds,
                SizeBytes = response.SizeBytes,
            });

            _host.ReportStatus(ResponseSummary);
        }
        catch (Exception ex)
        {
            ResponseSummary = "Request failed";
            ShowResponseBody(ex.Message);
            _host.ReportStatus(ResponseSummary);
        }
        finally
        {
            IsSending = false;
        }
    }

    private ApiRequest RunPreRequest(ApiRequest request, IDictionary<string, string> variables)
    {
        if (string.IsNullOrWhiteSpace(request.Script.PreRequest))
            return request;

        var headers = new Dictionary<string, string>();
        foreach (var header in request.Headers)
            headers[header.Name] = header.Value;

        var bodyText = request.Body.Type == BodyType.Raw ? request.Body.Text ?? string.Empty : string.Empty;
        var scriptRequest = new ScriptRequest(request.Url, request.Method, bodyText, headers);

        ReportScript(_scriptEngine.RunPreRequest(request.Script.PreRequest, scriptRequest, variables));

        return request with
        {
            Url = scriptRequest.url,
            Method = scriptRequest.method,
            Headers = headers.Select(h => new KeyValueItem(h.Key, h.Value)).ToList(),
            Body = request.Body.Type == BodyType.Raw ? request.Body with { Text = scriptRequest.body } : request.Body,
        };
    }

    private void RunPostResponse(ApiRequest request, ApiResponse response, IDictionary<string, string> variables)
    {
        if (string.IsNullOrWhiteSpace(request.Script.PostResponse))
            return;

        var responseHeaders = new Dictionary<string, string>();
        foreach (var header in response.Headers)
            responseHeaders[header.Name] = header.Value;

        var scriptRequest = new ScriptRequest(request.Url, request.Method, string.Empty, new Dictionary<string, string>());
        var scriptResponse = new ScriptResponse(response.StatusCode, response.Body, responseHeaders);

        ReportScript(_scriptEngine.RunPostResponse(request.Script.PostResponse, scriptRequest, scriptResponse, variables));
    }

    private void ReportScript(ScriptResult result)
    {
        foreach (var test in result.Tests)
            TestResults.Add(test);

        if (result.Error is not null)
            ScriptError = string.IsNullOrEmpty(ScriptError) ? result.Error : $"{ScriptError}\n{result.Error}";
    }

    private void PersistRuntimeVariables(IReadOnlyDictionary<string, string> variables)
    {
        foreach (var (key, value) in variables)
        {
            if (!Variables.TryGetValue(key, out var envValue) || envValue != value)
                _extracted[key] = value;
        }
    }

    private void ShowResponseBody(string body)
    {
        ResponseBody = body;
        ResponseTree.Clear();

        if (JsonFormatter.TryPrettify(body, out var pretty))
        {
            ResponsePretty = pretty;
            ResponseIsJson = true;
            try
            {
                ResponseTree.Add(JsonTree.Parse(body));
            }
            catch (System.Text.Json.JsonException)
            {
                // Prettify succeeded but tree parse failed — leave the tree empty.
            }
        }
        else
        {
            ResponsePretty = body;
            ResponseIsJson = false;
        }
    }

    private bool CanSend() => !IsSending && !string.IsNullOrWhiteSpace(Url);

    partial void OnIsSendingChanged(bool value) => SendCommand.NotifyCanExecuteChanged();

    partial void OnUrlChanged(string value) => SendCommand.NotifyCanExecuteChanged();

    /// <summary>Builds an <see cref="ApiRequest"/> from the current editor state.</summary>
    public ApiRequest ToRequest() => BuildRequest();

    /// <summary>Loads <paramref name="request"/> into the editor, replacing the current contents and clearing any prior response.</summary>
    public void LoadFrom(ApiRequest request)
    {
        RequestName = request.Name;
        Method = request.Method;
        Url = request.Url;

        Headers.Clear();
        foreach (var header in request.Headers)
            Headers.Add(new HeaderRowViewModel { Enabled = header.Enabled, Name = header.Name, Value = header.Value });

        PreRequestScript = request.Script.PreRequest;
        PostResponseScript = request.Script.PostResponse;
        ScriptError = string.Empty;
        TestResults.Clear();

        SelectedAuthType = request.Auth.Type;
        AuthToken = request.Auth.Token ?? string.Empty;
        AuthUsername = request.Auth.Username ?? string.Empty;
        AuthPassword = request.Auth.Password ?? string.Empty;
        AuthKeyName = request.Auth.ApiKeyName ?? string.Empty;
        AuthKeyValue = request.Auth.ApiKeyValue ?? string.Empty;
        AuthKeyLocation = request.Auth.ApiKeyLocation;

        if (request.Body.Type == BodyType.Raw)
        {
            RequestBody = request.Body.Text ?? string.Empty;
            if (!string.IsNullOrEmpty(request.Body.MediaType))
                BodyMediaType = request.Body.MediaType;
        }
        else
        {
            RequestBody = string.Empty;
        }

        ResponseSummary = string.Empty;
        ResponseHeaders = string.Empty;
        ShowResponseBody(string.Empty);
    }

    private ApiRequest BuildRequest()
    {
        var headers = Headers
            .Where(h => h.Enabled && !string.IsNullOrWhiteSpace(h.Name))
            .Select(h => new KeyValueItem(h.Name, h.Value))
            .ToList();

        var body = string.IsNullOrEmpty(RequestBody)
            ? new RequestBody()
            : new RequestBody { Type = BodyType.Raw, MediaType = BodyMediaType, Text = RequestBody };

        return new ApiRequest
        {
            Name = RequestName,
            Method = Method,
            Url = Url,
            Headers = headers,
            Body = body,
            Auth = new RequestAuth
            {
                Type = SelectedAuthType,
                Token = AuthToken,
                Username = AuthUsername,
                Password = AuthPassword,
                ApiKeyName = AuthKeyName,
                ApiKeyValue = AuthKeyValue,
                ApiKeyLocation = AuthKeyLocation,
            },
            Script = new RequestScript
            {
                PreRequest = PreRequestScript,
                PostResponse = PostResponseScript,
            },
        };
    }

    private static string FormatSize(long bytes)
        => bytes < 1024 ? $"{bytes} B" : $"{bytes / 1024.0:F1} KB";
}

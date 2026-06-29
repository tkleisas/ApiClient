using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using ApiClient.Core.CodeGen;
using ApiClient.Core.Hosting;
using ApiClient.Core.Http;
using ApiClient.Core.Model;
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
    private ICodeGenerator? _selectedGenerator;

    [ObservableProperty]
    private string _generatedCode = string.Empty;

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
        ResponseBody = string.Empty;

        try
        {
            var response = await _executor.ExecuteAsync(BuildRequest(), NoVariables);
            ResponseSummary =
                $"{response.StatusCode} {response.ReasonPhrase} · {response.Elapsed.TotalMilliseconds:F0} ms · {FormatSize(response.SizeBytes)}";
            ResponseHeaders = string.Join(Environment.NewLine, response.Headers.Select(h => $"{h.Name}: {h.Value}"));
            ResponseBody = response.Body;
            _host.ReportStatus(ResponseSummary);
        }
        catch (Exception ex)
        {
            ResponseSummary = "Request failed";
            ResponseBody = ex.Message;
            _host.ReportStatus(ResponseSummary);
        }
        finally
        {
            IsSending = false;
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
        ResponseBody = string.Empty;
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
        };
    }

    private static string FormatSize(long bytes)
        => bytes < 1024 ? $"{bytes} B" : $"{bytes / 1024.0:F1} KB";
}

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using ApiClient.Core.Http;
using ApiClient.Core.Model;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ApiClient.App.ViewModels;

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
/// The main editor: edits a single request (method, URL, headers, body), sends it through
/// the <see cref="ApiClient.Core"/> engine, and shows the response. Deliberately thin — all
/// real work happens in <see cref="RequestExecutor"/>.
/// </summary>
public partial class MainWindowViewModel : ViewModelBase
{
    private static readonly IReadOnlyDictionary<string, string> NoVariables = new Dictionary<string, string>();

    private readonly RequestExecutor _executor;

    /// <summary>Design-time / default constructor: wires the real HTTP engine.</summary>
    public MainWindowViewModel()
        : this(new RequestExecutor(HttpRequestFactory.CreateDefault(), new HttpClientSender(new HttpClient())))
    {
    }

    /// <summary>Creates the view model with an explicit executor (used for testing/DI).</summary>
    public MainWindowViewModel(RequestExecutor executor)
    {
        _executor = executor;
        Headers.Add(new HeaderRowViewModel { Name = "Accept", Value = "application/json" });
    }

    /// <summary>The HTTP methods offered in the method drop-down.</summary>
    public string[] Methods { get; } = ["GET", "POST", "PUT", "PATCH", "DELETE", "HEAD", "OPTIONS"];

    /// <summary>Editable request headers.</summary>
    public ObservableCollection<HeaderRowViewModel> Headers { get; } = [];

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
        }
        catch (Exception ex)
        {
            ResponseSummary = "Request failed";
            ResponseBody = ex.Message;
        }
        finally
        {
            IsSending = false;
        }
    }

    private bool CanSend() => !IsSending && !string.IsNullOrWhiteSpace(Url);

    partial void OnIsSendingChanged(bool value) => SendCommand.NotifyCanExecuteChanged();

    partial void OnUrlChanged(string value) => SendCommand.NotifyCanExecuteChanged();

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
            Name = "Untitled",
            Method = Method,
            Url = Url,
            Headers = headers,
            Body = body,
        };
    }

    private static string FormatSize(long bytes)
        => bytes < 1024 ? $"{bytes} B" : $"{bytes / 1024.0:F1} KB";
}

using System.Diagnostics;
using System.IO;

namespace ApiClient.Core.Hosting;

/// <summary>
/// Services the surrounding host provides to the API client UI, so the same UI behaves
/// correctly whether it runs standalone or embedded in another app (e.g. the nvs IDE).
/// Defined here in the UI-free core (no Avalonia types) so any host can implement it.
/// </summary>
public interface IHostServices
{
    /// <summary>The directory under which collections are stored/opened. A host (e.g. an IDE) typically points this at the open workspace.</summary>
    string CollectionsRoot { get; }

    /// <summary>Opens a file using whatever mechanism is appropriate for the host.</summary>
    void OpenFile(string path);

    /// <summary>Surfaces a short status message to the user (status bar, IDE status area, ...).</summary>
    void ReportStatus(string message);
}

/// <summary>
/// The default <see cref="IHostServices"/> used when running standalone: collections live
/// under the user's profile, files open with the OS default handler, and status messages
/// are recorded as <see cref="LastStatus"/> (a standalone shell can observe this).
/// </summary>
public sealed class StandaloneHostServices : IHostServices
{
    /// <inheritdoc/>
    public string CollectionsRoot { get; }

    /// <summary>The most recent status message reported, or empty if none.</summary>
    public string LastStatus { get; private set; } = string.Empty;

    /// <summary>Creates the standalone host services, optionally overriding the collections root.</summary>
    public StandaloneHostServices(string? collectionsRoot = null)
    {
        CollectionsRoot = collectionsRoot ?? Path.Combine(
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile),
            "ApiClient",
            "Collections");
    }

    /// <inheritdoc/>
    public void OpenFile(string path)
        => Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });

    /// <inheritdoc/>
    public void ReportStatus(string message) => LastStatus = message;
}

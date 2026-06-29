using System.Reflection;

namespace ApiClient.Core;

/// <summary>
/// Exposes the build version stamped into the assembly by MinVer (derived from the
/// nearest git tag). Both the standalone app and any host integration can display this
/// so the user always sees which version they are running.
/// </summary>
public static class BuildInfo
{
    /// <summary>
    /// The display version, e.g. <c>"1.2.3"</c> or <c>"1.2.4-alpha.0.5"</c>. Any build
    /// metadata after a <c>+</c> is trimmed off. Falls back to <c>"0.0.0"</c> if absent.
    /// </summary>
    public static string Version { get; } = ResolveVersion();

    private static string ResolveVersion()
    {
        var informational = typeof(BuildInfo).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

        if (string.IsNullOrEmpty(informational))
            return "0.0.0";

        var plus = informational.IndexOf('+');
        return plus >= 0 ? informational[..plus] : informational;
    }
}

using Avalonia;
using System;
using System.Threading.Tasks;
using ApiClient.Core;
using ApiClient.Core.Diagnostics;

namespace ApiClient.App;

sealed class Program
{
    /// <summary>The application-wide logger; records crashes and unhandled exceptions to a file.</summary>
    public static FileLogger Logger { get; } = new FileLogger(FileLogger.DefaultPath());

    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            Logger.Error("Unhandled exception", e.ExceptionObject as Exception);

        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            Logger.Error("Unobserved task exception", e.Exception);
            e.SetObserved();
        };

        try
        {
            Logger.Info($"ApiClient {BuildInfo.Version} starting");
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
            Logger.Info("ApiClient exited normally");
        }
        catch (Exception ex)
        {
            Logger.Error("Fatal error; application is terminating", ex);
            throw;
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace();
}

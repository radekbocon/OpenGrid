using Avalonia;
using Avalonia.Threading;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace OpenGrid;

sealed class Program
{
    private const byte ActivateWindowSignal = 0x45;
    public static IServiceProvider ServiceProvider { get; private set; } = null!;

    public static string AppDataDirectory { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "OpenGrid");
    private static FileStream? _instanceLock;
    private static TcpListener? _activationListener;
    public static Action? ActivateWindowRequested { get; set; }
    
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        // Mutex seems to not work on Linux, so use a file lock instead
        Directory.CreateDirectory(AppDataDirectory);
        var lockPath = Path.Combine(AppDataDirectory, "instance.lock");
        try
        {
            _instanceLock = new FileStream(lockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        }
        catch (IOException)
        {
            SignalRunningInstance();
            Log.Information("Another instance is already running; bringing to front");
            return;
        }

        // Start TCP listener for inter-process activation, should use dbus instead?
        try
        {
            _activationListener = new TcpListener(IPAddress.Loopback, 51234);
            _activationListener.Start();
            _ = Task.Run(ActivationListenerLoopAsync);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Could not start activation listener; bring-to-front won't be available");
        }

        // Background thread exceptions
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            Log.Fatal(e.ExceptionObject as Exception, "Unhandled domain exception");

        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            Log.Error(e.Exception, "Unobserved task exception");
            e.SetObserved();
        };
        
        var services = new ServiceCollection();
        services.AddOpenGridServices();
        ServiceProvider = services.BuildServiceProvider();

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
#if DEBUG
            .WriteTo.Debug()
#else
            .WriteTo.Console()
#endif
            .WriteTo.File(Path.Combine(AppDataDirectory, "logs", "opengrid.log"),
                rollingInterval: RollingInterval.Day)
            .CreateLogger();

        try
        {
            Log.Information("Starting application");
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        catch (Exception e)
        {
            Log.Fatal(e, "Application crashed");
        }
        finally
        {
            _activationListener?.Stop();
            _instanceLock.Dispose();
            Log.CloseAndFlush();
        }
    }

    private static void SignalRunningInstance()
    {
        try
        {
            using var client = new TcpClient();
            client.Connect(IPAddress.Loopback, 51234);
            client.GetStream().WriteByte(ActivateWindowSignal);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Could not signal running instance: {ex.Message}");
        }
    }

    private static async Task ActivationListenerLoopAsync()
    {
        if (_activationListener is not {} listener)
        {
            return;
        }

        try
        {
            while (true)
            {
                var handler = await listener.AcceptTcpClientAsync().ConfigureAwait(false);
                using (handler)
                {
                    var buffer = new byte[1];
                    var read = await handler.GetStream().ReadAsync(buffer, 0, 1).ConfigureAwait(false);
                    if (read > 0 && buffer[0] == ActivateWindowSignal)
                    {
                        Dispatcher.UIThread.Post(() => ActivateWindowRequested?.Invoke());
                    }
                }
            }
        }
        catch (ObjectDisposedException)
        {
            // Listener stopped during shutdown
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error in activation listener loop");
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    private static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<App>()
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace();
    }
}
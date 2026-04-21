using System.IO.MemoryMappedFiles;

namespace SimLabBridge;

/// <summary>
/// Bridge service for reading ACC telemetry data from shared memory
/// Handles Windows/Proton compatibility by mapping shared memory to Linux /dev/shm/ files
/// </summary>
public class AccSharedMemoryBridge : IDisposable
{
    private const string PHYSICS_BUFFER_NAME = "Local\\acpmf_physics";
    private const string GRAPHICS_BUFFER_NAME = "Local\\acpmf_graphics";
    private const string STATIC_BUFFER_NAME = "Local\\acpmf_static";

    private const string SHM_PHYSICS_PATH = "/dev/shm/simlab_physics";
    private const string SHM_GRAPHICS_PATH = "/dev/shm/simlab_graphics";
    private const string SHM_STATIC_PATH = "/dev/shm/simlab_static";

    private MemoryMappedFile? _physicsFile;
    private MemoryMappedFile? _graphicsFile;
    private MemoryMappedFile? _staticFile;

    private MemoryMappedViewAccessor? _physicsAccessor;
    private MemoryMappedViewAccessor? _graphicsAccessor;
    private MemoryMappedViewAccessor? _staticAccessor;

    private bool _disposed = false;
    private bool _isConnected = false;

    public bool IsConnected => _isConnected;

    /// <summary>
    /// Initialize the bridge and attempt connection to ACC shared memory
    /// </summary>
    public async Task<bool> InitializeAsync()
    {
        try
        {
            return await Task.Run(() => Initialize());
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error initializing: {ex.Message}");
            return false;
        }
    }

    private bool Initialize()
    {
        try
        {
            // Try to open existing memory mapped files from ACC
            _physicsFile = MemoryMappedFile.OpenExisting(PHYSICS_BUFFER_NAME, MemoryMappedFileRights.Read);
            _graphicsFile = MemoryMappedFile.OpenExisting(GRAPHICS_BUFFER_NAME, MemoryMappedFileRights.Read);
            _staticFile = MemoryMappedFile.OpenExisting(STATIC_BUFFER_NAME, MemoryMappedFileRights.Read);

            _physicsAccessor = _physicsFile.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read);
            _graphicsAccessor = _graphicsFile.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read);
            _staticAccessor = _staticFile.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read);

            _isConnected = true;
            Console.WriteLine("Connected to ACC");
            return true;
        }
        catch (FileNotFoundException)
        {
            Console.WriteLine("ACC shared memory not found. Is ACC running under Proton?");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Connection failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Read raw telemetry data from shared memory and write to shared files
    /// </summary>
    public void ReadAndWriteTelemetry()
    {
        if (!_isConnected || _physicsAccessor == null || _graphicsAccessor == null || _staticAccessor == null)
            return;

        try
        {
            var physicsBytes = ReadBytes(_physicsAccessor);
            var graphicsBytes = ReadBytes(_graphicsAccessor);
            var staticBytes = ReadBytes(_staticAccessor);

            WriteToSharedFiles(physicsBytes, graphicsBytes, staticBytes);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading/writing telemetry: {ex.Message}");
        }
    }

    /// <summary>
    /// Write raw telemetry data to shared files
    /// </summary>
    public void WriteToSharedFiles(byte[] physics, byte[] graphics, byte[] staticData)
    {
        try
        {
            File.WriteAllBytes(SHM_PHYSICS_PATH, physics);
            File.WriteAllBytes(SHM_GRAPHICS_PATH, graphics);
            File.WriteAllBytes(SHM_STATIC_PATH, staticData);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error writing to shared files: {ex.Message}");
        }
    }

    /// <summary>
    /// Start continuous telemetry reading loop
    /// </summary>
    public async Task StartReadingAsync(CancellationToken cancellationToken = default)
    {
        while (!await InitializeAsync())
        {
            await Task.Delay(1000, cancellationToken);
        }

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                ReadAndWriteTelemetry();

                await Task.Delay(16, cancellationToken); // ~60 Hz update rate
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when cancellation is requested
        }
        finally
        {
            Disconnect();
        }
    }

    /// <summary>
    /// Disconnect from ACC shared memory
    /// </summary>
    public void Disconnect()
    {
        _isConnected = false;
        Console.WriteLine("Disconnected");
    }

    /// <summary>
    /// Read bytes from a memory mapped file accessor
    /// </summary>
    private byte[] ReadBytes(MemoryMappedViewAccessor accessor)
    {
        byte[] buffer = new byte[accessor.Capacity];
        accessor.ReadArray(0, buffer, 0, buffer.Length);
        return buffer;
    }

    public void Dispose()
    {
        if (_disposed) return;

        _physicsAccessor?.Dispose();
        _graphicsAccessor?.Dispose();
        _staticAccessor?.Dispose();

        _physicsFile?.Dispose();
        _graphicsFile?.Dispose();
        _staticFile?.Dispose();

        _disposed = true;
    }
}
